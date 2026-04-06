using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Chat.Api;
using ChatRumi.Chat.Api.Hub;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto;
using ChatRumi.Chat.Application.Queries;
using ChatRumi.Chat.Infrastructure;
using ChatRumi.Infrastructure;
using ChatRumi.Infrastructure.Storage;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration, builder.Environment);
builder.Services.AddAttachmentFileStorage();

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
    [FromForm] IFormFile file,
    IAttachmentFileStorage fileStorage
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

    await using var input = file.OpenReadStream();
    var storedFile = await fileStorage.StoreFileAsync(
        bucket: "chat-attachments",
        originalFileName: file.FileName,
        contentType: file.ContentType,
        content: input,
        sizeBytes: file.Length,
        cancellationToken: http.RequestAborted
    );

    var url = $"/chat/attachments/{storedFile.StoredFileName}";
    var response = new ChatAttachmentUploadResponse(
        storedFile.AttachmentId,
        storedFile.OriginalFileName,
        storedFile.ContentType,
        storedFile.SizeBytes,
        url
    );

    return Results.Ok(response);
})
.DisableAntiforgery();

chatGroup.MapGet("/attachments/{fileName}", async (
    HttpContext http,
    [FromRoute] string fileName,
    IAttachmentFileStorage fileStorage
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

    var attachment = await fileStorage.GetFileAsync(
        bucket: "chat-attachments",
        fileName: fileName,
        cancellationToken: http.RequestAborted
    );
    if (attachment is null)
    {
        return Results.NotFound();
    }

    return Results.File(attachment.ContentStream, attachment.ContentType, enableRangeProcessing: true);
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