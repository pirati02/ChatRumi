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
}
