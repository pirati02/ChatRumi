using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRumi.Account.Api;
using ChatRumi.Account.Application;
using ChatRumi.Account.Application.Commands;
using ChatRumi.Account.Application.Queries;
using ChatRumi.Account.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using IMediator = MediatR.IMediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddApi(builder.Configuration, builder.Environment);
builder.Services.AddConsulService(builder.Configuration);

var app = builder.Build();

app.UseCors("CorsPolicy");
var accountGroup = app.MapGroup("/api/account");

accountGroup.MapPost("", async ([FromBody] CreateAccount.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return result.Match(
            value => Results.Created(value.ToString(), value.ToString()),
            Results.BadRequest
        );
    })
    .WithName("create-account")
    .WithOpenApi();

accountGroup.MapPut("{accountId:guid}", async ([FromRoute] Guid accountId, [FromBody] UpdateAccount.Command command, IMediator mediator) =>
    {
        command = command with
        {
            Id = accountId
        };
        var result = await mediator.Send(command);
        return result.Match(
            value => Results.Created(value.ToString(), value.ToString()),
            Results.BadRequest
        );
    })
    .WithName("update-account")
    .WithOpenApi();

accountGroup.MapPut("activate", async ([FromBody] VerifyAccount.Command request, IMediator mediator) =>
    {
        var result = await mediator.Send(request);
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("activate-account")
    .WithOpenApi();

accountGroup.MapPatch("{accountId:guid}/resend-code", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new SendVerificationRequest.Command(accountId));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("verify-account")
    .WithOpenApi();

accountGroup.MapGet("{accountId:guid}", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new GetAccount.Query(accountId));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("get-account")
    .WithOpenApi();

accountGroup.MapGet("", async (IMediator mediator) =>
    {
        var result = await mediator.Send(new GetAccounts.Query());
        return Results.Ok(result);
    })
    .WithName("get-accounts")
    .WithOpenApi();

accountGroup.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("account-health");

await app.RunAsync();