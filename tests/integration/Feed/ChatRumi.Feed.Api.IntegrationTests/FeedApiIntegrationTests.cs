using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChatRumi.Feed.Api;
using ChatRumi.Feed.Application.Commands;
using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Feed.Domain.ValueObject;
using ChatRumi.Infrastructure;
using ChatRumi.IntegrationTesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.Elasticsearch;
using Xunit;

namespace ChatRumi.Feed.Api.IntegrationTests;

public sealed class FeedApiIntegrationTests : IAsyncLifetime
{
    private ElasticsearchContainer? _elastic;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private Guid _creatorId;

    public async Task InitializeAsync()
    {
        _elastic = new ElasticsearchBuilder()
            .WithImage("elasticsearch:7.17.21")
            .WithEnvironment("discovery.type", "single-node")
            .WithEnvironment("xpack.security.enabled", "false")
            .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
            .Build();

        await _elastic.StartAsync();

        var elasticUri = _elastic.GetConnectionString();
        if (elasticUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            elasticUri = "http://" + elasticUri["https://".Length..];

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureIntegrationTestLogging();
                builder.UseEnvironment("Integration");
                builder.UseSetting("ConnectionStrings:FeedContext", elasticUri);
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        _creatorId = Guid.NewGuid();
        var token = IntegrationTestJwt.CreateAccessToken(jwt, _creatorId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory?.DisposeAsync().AsTask()!;

        if (_elastic is not null)
            await _elastic.DisposeAsync();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _factory!.CreateClient().GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_post_indexes_in_elasticsearch()
    {
        var command = new CreatePost.Command(
            new Participant
            {
                Id = _creatorId,
                FirstName = "Fn",
                LastName = "Ln",
                NickName = "nn"
            },
            Title: "integration-title",
            Description: "integration-desc");

        var response = await _client!.PostAsJsonAsync("/api/feed", command);
        response.EnsureSuccessStatusCode();
        var id = await response.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(id);
        Assert.NotEmpty(id);

        var get = await _client!.GetAsync($"/api/feed/{id}");
        get.EnsureSuccessStatusCode();
        var roundTrip = await get.Content.ReadFromJsonAsync<PostDocument>();
        Assert.NotNull(roundTrip);
        Assert.Equal("integration-title", roundTrip.Title);
        Assert.Equal("integration-desc", roundTrip.Description);
    }

    [Fact]
    public async Task Toggle_post_reaction_adds_and_removes_same_reaction()
    {
        var postId = await CreatePostAndGetId();

        var togglePayload = new
        {
            actor = new Participant
            {
                Id = _creatorId,
                FirstName = "Fn",
                LastName = "Ln",
                NickName = "nn"
            },
            reactionType = ReactionType.Like
        };

        var addResponse = await _client!.PutAsJsonAsync($"/api/feed/{postId}/reactions", togglePayload);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, addResponse.StatusCode);

        var getAfterAdd = await _client.GetFromJsonAsync<PostDocument>($"/api/feed/{postId}");
        Assert.NotNull(getAfterAdd);
        Assert.Single(getAfterAdd.Reactions);
        Assert.Equal(ReactionType.Like, getAfterAdd.Reactions[0].ReactionType);

        var removeResponse = await _client.PutAsJsonAsync($"/api/feed/{postId}/reactions", togglePayload);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, removeResponse.StatusCode);

        var getAfterRemove = await _client.GetFromJsonAsync<PostDocument>($"/api/feed/{postId}");
        Assert.NotNull(getAfterRemove);
        Assert.Empty(getAfterRemove.Reactions);
    }

    [Fact]
    public async Task Add_comment_reply_and_comment_reaction_are_returned_in_post_details()
    {
        var postId = await CreatePostAndGetId();
        var commentPayload = new
        {
            creator = new Participant
            {
                Id = _creatorId,
                FirstName = "Fn",
                LastName = "Ln",
                NickName = "nn"
            },
            content = "Parent comment"
        };

        var commentResponse = await _client!.PostAsJsonAsync($"/api/feed/{postId}/comments", commentPayload);
        commentResponse.EnsureSuccessStatusCode();
        var commentId = await commentResponse.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, commentId);

        var replyPayload = new
        {
            creator = commentPayload.creator,
            content = "A reply"
        };
        var replyResponse = await _client.PostAsJsonAsync($"/api/feed/{postId}/comments/{commentId}/replies", replyPayload);
        replyResponse.EnsureSuccessStatusCode();

        var toggleCommentReactionPayload = new
        {
            actor = commentPayload.creator,
            reactionType = ReactionType.Heart
        };
        var toggleReactionResponse = await _client.PutAsJsonAsync($"/api/feed/comments/{commentId}/reactions", toggleCommentReactionPayload);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, toggleReactionResponse.StatusCode);

        var detailsResponse = await _client.GetAsync($"/api/feed/{postId}/details");
        detailsResponse.EnsureSuccessStatusCode();
        var details = await detailsResponse.Content.ReadFromJsonAsync<PostDetailsDocument>();
        Assert.NotNull(details);
        Assert.Equal(postId, details.Post.Id);
        Assert.Single(details.Comments);
        Assert.Single(details.Comments[0].Replies);
        Assert.Single(details.Comments[0].Comment.Reactions);
        Assert.Equal(ReactionType.Heart, details.Comments[0].Comment.Reactions[0].ReactionType);
    }

    private async Task<Guid> CreatePostAndGetId()
    {
        var command = new CreatePost.Command(
            new Participant
            {
                Id = _creatorId,
                FirstName = "Fn",
                LastName = "Ln",
                NickName = "nn"
            },
            Title: "integration-title",
            Description: "integration-desc");

        var response = await _client!.PostAsJsonAsync("/api/feed", command);
        response.EnsureSuccessStatusCode();
        var id = await response.Content.ReadFromJsonAsync<string>();
        Assert.False(string.IsNullOrWhiteSpace(id));
        return Guid.Parse(id!);
    }
}
