using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Feed.Api;
using ChatRumi.Feed.Application;
using ChatRumi.Feed.Application.Commands;
using ChatRumi.Feed.Application.Queries;
using ChatRumi.Feed.Infrastructure;
using Mediator;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddPresentation();
builder.Services.AddApplication();
builder.Services.AddConsulService(builder.Configuration);
builder.Services.AddOpenTelemetryObservability(builder.Configuration);

var app = builder.Build();

// Add request/response body logging for OpenTelemetry (must be early in pipeline)
app.UseRequestResponseLogging(
    maxBodySize: 8192,  // 8KB max body capture
    excludedPaths: "/health");

await PostIndexer.IndexPost(app.Services);
app.UseCors("CorsPolicy");
app.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("feed-health");

var feedGroup = app.MapGroup("/api/feed");

feedGroup.MapGet("{id:guid}", async (Guid id, IMediator mediator) =>
    {
        var result = await mediator.Send(new GetPost.Query(id));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("get-post");

feedGroup.MapGet("shuffled/{creatorId:guid}", async ([FromRoute] Guid creatorId, [FromQuery] int limit, IMediator mediator) =>
    {
        var result = await mediator.Send(new GetPosts.Query(creatorId, limit));
        return Results.Ok(result);
    })
    .WithName("get-posts");

feedGroup.MapPost("", async ([FromBody] CreatePost.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return result.Match(
            value => Results.Created($"/api/feed/{value}", value),
            Results.InternalServerError
        );
    })
    .WithName("create-post");


app.Run();