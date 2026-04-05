using ChatRum.Gateway;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Infrastructure;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

var builder = WebApplication.CreateBuilder(args);

var ocelotJson = builder.Environment.IsEnvironment("Local")
    ? "ocelot.Local.json"
    : "ocelot.json";

builder.Configuration.AddJsonFile(ocelotJson, optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot()
    .AddConsul<ChatRumConsulServiceBuilder>();

builder.Services.AddOpenTelemetryObservability(builder.Configuration);

builder.Services.AddChatRumiResponseCompression();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policyBuilder =>
    {
        policyBuilder.WithOrigins(
                "http://localhost:4200",
                "http://gateway:7000",
                "http://localhost:7000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseWebSockets();
app.UseResponseCompression();

// Ocelot runs as middleware and does not forward unmatched routes to minimal API endpoints, so /health must be
// handled before UseOcelot (Aspire WithHttpHealthCheck uses GET /health).
app.Use(async (context, next) =>
{
    if (context.Request.Method != HttpMethods.Get)
    {
        await next();
        return;
    }

    if (context.Request.Path == "/")
    {
        await context.Response.WriteAsync("Gateway is running");
        return;
    }

    if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
    {
        await context.Response.WriteAsync("Healthy ✅");
        return;
    }

    await next();
});

await app.UseOcelot();
await app.RunAsync();