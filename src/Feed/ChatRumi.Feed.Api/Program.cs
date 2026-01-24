using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRumi.Feed.Api;
using ChatRumi.Feed.Application;
using ChatRumi.Feed.Application.Commands;
using ChatRumi.Feed.Application.Queries;
using ChatRumi.Feed.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nest;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddPresentation();
builder.Services.AddApplication();
builder.Services.AddConsulService(builder.Configuration);

var app = builder.Build();

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