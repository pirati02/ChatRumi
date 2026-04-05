using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChatRumi.Friendship.Api;
using ChatRumi.Friendship.Application.Dto.Request;
using ChatRumi.Infrastructure;
using ChatRumi.IntegrationTesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.Neo4j;
using Xunit;

namespace ChatRumi.Friendship.Api.IntegrationTests;

public sealed class FriendshipApiIntegrationTests : IAsyncLifetime
{
    private Neo4jContainer? _neo4j;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private Guid _peerId;

    public async Task InitializeAsync()
    {
        const string neo4jPassword = "Passw0rd!Test";

        _neo4j = new Neo4jBuilder()
            .WithImage("neo4j:5")
            .WithEnvironment("NEO4J_AUTH", $"neo4j/{neo4jPassword}")
            .Build();

        await _neo4j.StartAsync();

        var boltUri = _neo4j.GetConnectionString();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureIntegrationTestLogging();
                builder.UseEnvironment("Integration");
                builder.UseSetting("Neo4jOptions:Neo4jConnection", boltUri);
                builder.UseSetting("Neo4jOptions:Neo4jUser", "neo4j");
                builder.UseSetting("Neo4jOptions:Neo4jPassword", neo4jPassword);
                builder.UseSetting("Neo4jOptions:Neo4jDatabase", "neo4j");
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        _peerId = Guid.NewGuid();
        var token = IntegrationTestJwt.CreateAccessToken(jwt, _peerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory?.DisposeAsync().AsTask()!;

        if (_neo4j is not null)
            await _neo4j.DisposeAsync();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _factory!.CreateClient().GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Friends_list_queries_neo4j()
    {
        var response = await _client!.GetAsync($"/api/friendship/{_peerId}");
        response.EnsureSuccessStatusCode();
        var friends = await response.Content.ReadFromJsonAsync<PeerResponse[]>();
        Assert.NotNull(friends);
        Assert.Empty(friends);
    }
}
