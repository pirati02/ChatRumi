using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Account.Api;
using ChatRumi.Account.Application;
using ChatRumi.Account.Application.Commands;
using ChatRumi.Account.Application.Queries;
using ChatRumi.Account.Infrastructure;
using ChatRumi.Infrastructure;
using ChatRumi.Kernel.Results;
using Mediator;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            ip,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddHostedService<AccountOutboxRelayBackgroundService>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseChatRumiHttpsRedirectionAndHsts();
app.UseChatRumiSecurityHeaders();
app.UseResponseCompression();
// Add request/response body logging for OpenTelemetry (must be early in pipeline)
app.UseRequestResponseLogging(
    maxBodySize: 8192,  // 8KB max body capture
    excludedPaths: "/health");

app.UseCors("CorsPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("account-health")
    .AllowAnonymous();

var accountGroup = app.MapGroup("/api/account").RequireAuthorization();

accountGroup.MapPost("login", async ([FromBody] Login.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return result switch
        {
            AuthTokenResponse token => Results.Ok(token),
            IEnumerable<ResponseError> errors when errors.Any(e => e.Type == ErrorType.Unauthorized) =>
                Results.Json(
                    new { message = "Invalid email or password." },
                    statusCode: StatusCodes.Status401Unauthorized),
            IEnumerable<ResponseError> errors => Results.BadRequest(errors)
        };
    })
    .WithName("login")
    .RequireRateLimiting("auth")
    .AllowAnonymous();

accountGroup.MapPost("refresh", async ([FromBody] Refresh.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return result switch
        {
            AuthTokenResponse token => Results.Ok(token),
            IEnumerable<ResponseError> errors when errors.Any(e => e.Type == ErrorType.Unauthorized) =>
                Results.Json(
                    new { message = "Invalid or expired refresh token." },
                    statusCode: StatusCodes.Status401Unauthorized),
            IEnumerable<ResponseError> errors => Results.BadRequest(errors)
        };
    })
    .WithName("refresh")
    .RequireRateLimiting("auth")
    .AllowAnonymous();

accountGroup.MapPost("", async ([FromBody] Register.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return result switch
        {
            Guid id => Results.Created(id.ToString(), id.ToString()),
            IEnumerable<ResponseError> errors => Results.BadRequest(errors)
        };
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
        return result switch
        {
            Guid id => Results.Created(id.ToString(), id.ToString()),
            IEnumerable<ResponseError> errors => Results.BadRequest(errors)
        };
    })
    .WithName("update-account");

accountGroup.MapPut("activate", async ([FromBody] VerifyAccount.Command request, IMediator mediator) =>
    {
        var result = await mediator.Send(request);
        return result switch
        {
            bool verified => Results.Ok(verified),
            IEnumerable<ResponseError> errors => Results.NotFound(errors)
        };
    })
    .WithName("activate-account")
    .AllowAnonymous();

accountGroup.MapPatch("{accountId:guid}/resend-code", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new SendVerificationRequest.Command(accountId));
        return result switch
        {
            bool sent => Results.Ok(sent),
            IEnumerable<ResponseError> errors => Results.NotFound(errors)
        };
    })
    .WithName("verify-account")
    .AllowAnonymous();

accountGroup.MapGet("{accountId:guid}", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new GetAccount.Query(accountId));
        return result switch
        {
            AccountResponse account => Results.Ok(account),
            IEnumerable<ResponseError> errors => Results.NotFound(errors)
        };
    })
    .WithName("get-account");

accountGroup.MapGet("", async (IMediator mediator) =>
    {
        var result = await mediator.Send(new GetAccounts.Query());
        return Results.Ok(result);
    })
    .WithName("get-accounts");

await app.RunAsync();
