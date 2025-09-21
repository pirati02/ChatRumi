using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

var builder = WebApplication.CreateBuilder(args);

var ocelotJson = builder.Environment.IsEnvironment("Local")
    ? "ocelot.Local.json"
    : "ocelot.json";

builder.Configuration.AddJsonFile(ocelotJson, optional: false, reloadOnChange: true);

builder.Services.AddOcelot()
    .AddConsul();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");
app.MapGet("/", () => Results.Ok("Gateway is running"));

// Run Ocelot middleware
await app.UseOcelot();
await app.RunAsync();