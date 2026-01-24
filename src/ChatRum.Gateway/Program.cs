using ChatRum.Gateway;
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseWebSockets();
app.MapGet("/", () => Results.Ok("Gateway is running"));

await app.UseOcelot();
await app.RunAsync();