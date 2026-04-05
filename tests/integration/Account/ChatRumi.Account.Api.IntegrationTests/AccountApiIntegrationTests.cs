using System.Linq;
using System.Net.Http.Json;
using ChatRum.InterCommunication;
using ChatRumi.Account.Api;
using ChatRumi.Account.Application.Commands;
using ChatRumi.IntegrationTesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using WireMock.Server;
using Xunit;

namespace ChatRumi.Account.Api.IntegrationTests;

public sealed class AccountApiIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private WireMockServer? _wireMock;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("accountdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        _redis = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _postgres.StartAsync();
        await _redis.StartAsync();

        _wireMock = WireMockServer.Start();
        SmsOfficeWireMock.SetupSuccessfulSend(_wireMock);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureIntegrationTestLogging();
                builder.UseEnvironment("Integration");
                builder.UseSetting("ConnectionStrings:accountDatabase", _postgres.GetConnectionString());
                builder.UseSetting("RedisOptions:Host", "127.0.0.1");
                builder.UseSetting("RedisOptions:Port", _redis.GetMappedPublicPort(6379).ToString());
                builder.UseSetting("SmsOfficeOptions:BaseUrl", _wireMock.Url ?? "");
                builder.UseSetting("SmsOfficeOptions:ApiKey", "integration-test-key");
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IDispatcher, NoOpDispatcher>();
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory?.DisposeAsync().AsTask()!;
        _wireMock?.Dispose();

        if (_redis is not null)
            await _redis.DisposeAsync();

        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client!.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_and_resend_code_triggers_sms_stub()
    {
        var register = new Register.Command(
            UserName: "intuser1",
            Email: "intuser1@example.com",
            FirstName: "Int",
            LastName: "User",
            CountryCode: "+1",
            PhoneNumber: "5551234567",
            Password: "Hello1!x");

        var createResponse = await _client!.PostAsJsonAsync("/api/account", register);
        createResponse.EnsureSuccessStatusCode();
        var accountIdString = await createResponse.Content.ReadAsStringAsync();
        var accountId = Guid.Parse(accountIdString.Trim('"'));

        var resend = await _client!.PatchAsync($"/api/account/{accountId}/resend-code", null);
        resend.EnsureSuccessStatusCode();

        await WaitForSmsRequestsAsync(minCount: 2, TimeSpan.FromSeconds(30));

        Assert.All(_wireMock!.LogEntries, e => Assert.Contains("/api/v2/send", e.RequestMessage.Path, StringComparison.Ordinal));
    }

    private async Task WaitForSmsRequestsAsync(int minCount, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_wireMock!.LogEntries.Count() >= minCount)
                return;
            await Task.Delay(100);
        }

        Assert.Fail($"Expected at least {minCount} WireMock requests within {timeout.TotalSeconds}s.");
    }
}
