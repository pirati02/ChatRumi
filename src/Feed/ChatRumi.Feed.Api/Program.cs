using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRumi.Feed.Api;
using ChatRumi.Feed.Application;
using ChatRumi.Feed.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddConsulService(builder.Configuration);

var app = builder.Build();
  
app.UseCors("CorsPolicy");
 
var feedGroup = app.MapGroup("/api/feed");

feedGroup.MapGet("{id:guid}", async (Guid id, IMediator mediator) =>
    {
        var result = await mediator.Send(new GetPost.Query(id));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("get-post")
    .WithOpenApi();


feedGroup.MapPost("", async ([FromBody] CreatePost.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return Results.Created($"/api/feed/{result}", result);
    })
    .WithName("create-post")
    .WithOpenApi();
 
feedGroup.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("feed-health");

app.Run();
 