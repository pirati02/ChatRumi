using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot()
    .AddConsul();

var app = builder.Build();

// Optional: Add root status check
app.MapGet("/", () => Results.Ok("Gateway is running"));

// Run Ocelot middleware
await app.UseOcelot();
await app.RunAsync();