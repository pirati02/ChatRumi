using System.Net.Http.Headers;
using System.Text.Json;
using ChatRumi.Chat.Api;
using ChatRumi.Infrastructure;
using ChatRumi.IntegrationTesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace ChatRumi.Chat.Api.IntegrationTests;

public sealed class ChatApiIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("chatdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        _redis = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _postgres.StartAsync();
        await _redis.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureIntegrationTestLogging();
                builder.UseEnvironment("Integration");
                builder.UseSetting("ConnectionStrings:chatDatabase", _postgres.GetConnectionString());
                builder.UseSetting("RedisOptions:Host", "127.0.0.1");
                builder.UseSetting("RedisOptions:Port", _redis.GetMappedPublicPort(6379).ToString());
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        var token = IntegrationTestJwt.CreateAccessToken(jwt, Guid.NewGuid());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory?.DisposeAsync().AsTask()!;

        if (_redis is not null)
            await _redis.DisposeAsync();

        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _factory!.CreateClient().GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Top10_latest_chats_queries_database()
    {
        var participantId = Guid.NewGuid();
        var response = await _client!.GetAsync($"/api/chat/{participantId}/top10");
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }
}
