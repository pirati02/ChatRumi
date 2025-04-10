using ChatRumi.Account.Api;
using ChatRumi.Account.Application;
using ChatRumi.Account.Application.Commands;
using ChatRumi.Account.Application.Queries;
using Microsoft.AspNetCore.Mvc;
using IMediator = MediatR.IMediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApi(builder.Configuration, builder.Environment);
builder.Services.AddApplication();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.MapPost("", async ([FromBody] CreateAccount.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return result.Match(
            value => Results.Created(value.ToString(), value.ToString()),
            Results.BadRequest
        );
    })
    .WithName("create-account")
    .WithOpenApi();

app.MapPut("activate", async ([FromBody] VerifyAccount.Command request, IMediator mediator) =>
    {
        var result = await mediator.Send(request);
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("activate-account")
    .WithOpenApi();

app.MapPatch("{accountId:guid}", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new VerificationRequest.Command(accountId));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("verify-account")
    .WithOpenApi();

app.MapGet("{accountId:guid}", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new GetAccount.Query(accountId));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("get-account")
    .WithOpenApi();

app.MapGet("", async (IMediator mediator) =>
    {
        var result = await mediator.Send(new GetAccounts.Query());
        return Results.Ok(result);
    })
    .WithName("get-accounts")
    .WithOpenApi();

await app.RunAsync();