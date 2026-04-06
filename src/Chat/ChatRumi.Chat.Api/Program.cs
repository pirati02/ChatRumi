using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Chat.Api;
using ChatRumi.Chat.Api.Hub;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto;
using ChatRumi.Chat.Application.Queries;
using ChatRumi.Chat.Infrastructure;
using ChatRumi.Infrastructure;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseChatRumiHttpsRedirectionAndHsts();
app.UseChatRumiSecurityHeaders();
app.UseResponseCompression();
// Add request/response body logging for OpenTelemetry (must be early in pipeline)
app.UseRequestResponseLogging(
    maxBodySize: 8192,  // 8KB max body capture
    excludedPaths: "/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("chat-health")
    .AllowAnonymous();

var chatGroup = app.MapGroup("/api/chat").RequireAuthorization();
const long MaxAttachmentSizeBytes = 20 * 1024 * 1024; // 20 MB

chatGroup.MapGet("/{participantId:guid}/top10", async (
    HttpContext http,
    [FromRoute] Guid participantId,
    IMediator mediator
) =>
{
    if (!http.User.TryGetAccountId(out var callerId) || callerId != participantId)
    {
        return Results.Forbid();
    }

    var result = await mediator.Send(new GetTop10LatestChat.Query(participantId));
    return result.Match(Results.Ok, Results.NotFound);
});

chatGroup.MapGet("{chatId:guid}", async (
    HttpContext http,
    [FromRoute] Guid chatId,
    IMediator mediator
) =>
{
    if (!http.User.TryGetAccountId(out var callerId))
    {
        return Results.Unauthorized();
    }

    var result = await mediator.Send(new GetChatById.Query(chatId, callerId));
    return result.Match(
        Results.Ok,
        errors => errors.Any(e => e.Type == ErrorType.Forbidden) ? Results.Forbid() : Results.NotFound());
});

chatGroup.MapPost("/search-existing", async (
    HttpContext http,
    [FromBody] ParticipantDto[] participants,
    IMediator mediator
) =>
{
    if (!http.User.TryGetAccountId(out var callerId))
    {
        return Results.Unauthorized();
    }

    var result = await mediator.Send(new SearchExistingChatByParticipant.Query(participants, callerId));
    return result.Match(
        Results.Ok,
        errors => errors.Any(e => e.Type == ErrorType.Forbidden) ? Results.Forbid() : Results.NotFound());
});

chatGroup.MapPost("/mark-as-read/{chatId:guid}", async (
    HttpContext http,
    [FromRoute] Guid chatId,
    [FromBody] Guid[] messageIds,
    IMediator mediator
) =>
{
    if (!http.User.TryGetAccountId(out var callerId))
    {
        return Results.Unauthorized();
    }

    var result = await mediator.Send(new MarkChatRead.Command(chatId, messageIds, callerId));
    return result.Match(
        Results.Ok,
        errors => errors.Any(e => e.Type == ErrorType.Forbidden) ? Results.Forbid() : Results.NotFound());
});

chatGroup.MapPost("/attachments", async (
    HttpContext http,
    [FromForm] IFormFile file
) =>
{
    if (!http.User.TryGetAccountId(out _))
    {
        return Results.Unauthorized();
    }

    if (file is null || file.Length == 0)
    {
        return Results.BadRequest("Attachment file is required.");
    }

    if (file.Length > MaxAttachmentSizeBytes)
    {
        return Results.BadRequest($"Attachment exceeds max size of {MaxAttachmentSizeBytes} bytes.");
    }

    var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "uploads", "chat-attachments");
    Directory.CreateDirectory(uploadsRoot);

    var attachmentId = Guid.CreateVersion7().ToString();
    var safeName = Path.GetFileName(file.FileName);
    var extension = Path.GetExtension(safeName);
    var storageName = $"{attachmentId}{extension}";
    var storagePath = Path.Combine(uploadsRoot, storageName);

    await using (var output = File.Create(storagePath))
    {
        await file.CopyToAsync(output);
    }

    var url = $"/chat/attachments/{attachmentId}{extension}";
    var response = new ChatAttachmentUploadResponse(
        attachmentId,
        safeName,
        string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
        file.Length,
        url
    );

    return Results.Ok(response);
})
.DisableAntiforgery();

chatGroup.MapGet("/attachments/{fileName}", async (
    HttpContext http,
    [FromRoute] string fileName
) =>
{
    if (!http.User.TryGetAccountId(out _))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(fileName))
    {
        return Results.BadRequest();
    }

    var safeFileName = Path.GetFileName(fileName);
    var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "uploads", "chat-attachments");
    var storagePath = Path.Combine(uploadsRoot, safeFileName);
    if (!File.Exists(storagePath))
    {
        return Results.NotFound();
    }

    var contentTypeProvider = new FileExtensionContentTypeProvider();
    var contentType = contentTypeProvider.TryGetContentType(safeFileName, out var resolvedContentType)
        ? resolvedContentType
        : "application/octet-stream";

    var stream = File.OpenRead(storagePath);
    return Results.File(stream, contentType, enableRangeProcessing: true);
});

app.MapHub<ChatHub>("/hub/chat").RequireAuthorization();

await app.RunAsync();

internal sealed record ChatAttachmentUploadResponse(
    string Id,
    string FileName,
    string MimeType,
    long SizeBytes,
    string Url
);