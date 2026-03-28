using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Account.Api;
using ChatRumi.Account.Application;
using ChatRumi.Account.Application.Commands;
using ChatRumi.Account.Application.Queries;
using ChatRumi.Account.Infrastructure;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration);

var app = builder.Build();

// Add request/response body logging for OpenTelemetry (must be early in pipeline)
app.UseRequestResponseLogging(
    maxBodySize: 8192,  // 8KB max body capture
    excludedPaths: "/health");

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("account-health")
    .AllowAnonymous();

var accountGroup = app.MapGroup("/api/account").RequireAuthorization();

accountGroup.MapPost("login", async ([FromBody] Login.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return result.Match(
            Results.Ok,
            errors =>
            {
                if (errors.Any(e => e.Type == ErrorType.Unauthorized))
                {
                    return Results.Json(
                        new { message = "Invalid email or password." },
                        statusCode: StatusCodes.Status401Unauthorized);
                }

                return Results.BadRequest(errors);
            });
    })
    .WithName("login")
    .AllowAnonymous();

accountGroup.MapPost("refresh", async ([FromBody] Refresh.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return result.Match(
            Results.Ok,
            errors =>
            {
                if (errors.Any(e => e.Type == ErrorType.Unauthorized))
                {
                    return Results.Json(
                        new { message = "Invalid or expired refresh token." },
                        statusCode: StatusCodes.Status401Unauthorized);
                }

                return Results.BadRequest(errors);
            });
    })
    .WithName("refresh")
    .AllowAnonymous();

accountGroup.MapPost("", async ([FromBody] Register.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return result.Match(
            value => Results.Created(value.ToString(), value.ToString()),
            Results.BadRequest
        );
    })
    .WithName("register-account")
    .AllowAnonymous();

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
    .WithName("update-account");

accountGroup.MapPut("activate", async ([FromBody] VerifyAccount.Command request, IMediator mediator) =>
    {
        var result = await mediator.Send(request);
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("activate-account")
    .AllowAnonymous();

accountGroup.MapPatch("{accountId:guid}/resend-code", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new SendVerificationRequest.Command(accountId));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("verify-account")
    .AllowAnonymous();

accountGroup.MapGet("{accountId:guid}", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new GetAccount.Query(accountId));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("get-account");

accountGroup.MapGet("", async (IMediator mediator) =>
    {
        var result = await mediator.Send(new GetAccounts.Query());
        return Results.Ok(result);
    })
    .WithName("get-accounts");

accountGroup.MapPut("{accountId:guid}/public-key", async (
        [FromRoute] Guid accountId,
        [FromBody] RegisterPublicKeyRequest request,
        IMediator mediator) =>
    {
        var result = await mediator.Send(new RegisterPublicKey.Command(accountId, request.PublicKey));
        return result.Match(
            _ => Results.Ok(),
            Results.NotFound
        );
    })
    .WithName("register-public-key");

// 404 if account missing; 200 with { publicKey: null } if account exists but no key registered.
accountGroup.MapGet("{accountId:guid}/public-key", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new GetPublicKey.Query(accountId));
        return result.Match(Results.Ok, Results.NotFound);
    })
    .WithName("get-public-key");

await app.RunAsync();
