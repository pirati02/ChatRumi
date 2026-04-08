using ChatRum.InterCommunication;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Infrastructure;
using ChatRumi.Notification.Api;
using ChatRumi.Notification.Api.Hub;
using ChatRumi.Notification.Application;
using ChatRumi.Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<KafkaOptions>().BindConfiguration(KafkaOptions.Name);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddHostedService<NotificationTriggeredConsumerBackgroundService>();

var app = builder.Build();

app.UseChatRumiHttpsRedirectionAndHsts();
app.UseChatRumiSecurityHeaders();
app.UseResponseCompression();
app.UseRequestResponseLogging(maxBodySize: 8192, excludedPaths: "/health");

await NotificationIndexer.EnsureIndex(app.Services);
app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("notification-health")
    .AllowAnonymous();

var notifications = app.MapGroup("/api/notifications").RequireAuthorization();

notifications.MapGet("", async (
    HttpContext http,
    DateTimeOffset? cursor,
    int? pageSize,
    INotificationService notificationService
) =>
{
    if (!http.User.TryGetAccountId(out var callerId))
    {
        return Results.Unauthorized();
    }

    var response = await notificationService.GetPageAsync(callerId, cursor, pageSize ?? 20, http.RequestAborted);
    return Results.Ok(response);
});

notifications.MapGet("/unread-count", async (HttpContext http, INotificationService notificationService) =>
{
    if (!http.User.TryGetAccountId(out var callerId))
    {
        return Results.Unauthorized();
    }

    var unreadCount = await notificationService.GetUnreadCountAsync(callerId, http.RequestAborted);
    return Results.Ok(new { unreadCount });
});

notifications.MapPost("/{id:guid}/read", async (HttpContext http, Guid id, INotificationService notificationService) =>
{
    if (!http.User.TryGetAccountId(out var callerId))
    {
        return Results.Unauthorized();
    }

    var ok = await notificationService.MarkReadAsync(callerId, id, http.RequestAborted);
    return ok ? Results.NoContent() : Results.NotFound();
});

notifications.MapPost("/read-all", async (HttpContext http, INotificationService notificationService) =>
{
    if (!http.User.TryGetAccountId(out var callerId))
    {
        return Results.Unauthorized();
    }

    var updated = await notificationService.MarkAllReadAsync(callerId, http.RequestAborted);
    return Results.Ok(new { updated });
});

app.MapHub<NotificationHub>("/hub/notifications").RequireAuthorization();

await app.RunAsync();
