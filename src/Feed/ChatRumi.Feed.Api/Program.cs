using ChatRum.InterCommunication.Telemetry;
using ChatRum.InterCommunication;
using ChatRumi.Feed.Api;
using ChatRumi.Feed.Application;
using ChatRumi.Feed.Application.Commands;
using ChatRumi.Feed.Application.Queries;
using ChatRumi.Feed.Domain.ValueObject;
using ChatRumi.Feed.Infrastructure;
using ChatRumi.Infrastructure;
using ChatRumi.Infrastructure.Storage;
using Mediator;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<KafkaOptions>().BindConfiguration(KafkaOptions.Name);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddAttachmentFileStorage();
builder.Services.AddHostedService<FeedOutboxRelayBackgroundService>();

var app = builder.Build();

app.UseChatRumiHttpsRedirectionAndHsts();
app.UseChatRumiSecurityHeaders();
app.UseResponseCompression();
// Add request/response body logging for OpenTelemetry (must be early in pipeline)
app.UseRequestResponseLogging(
    maxBodySize: 8192,  // 8KB max body capture
    excludedPaths: "/health");

await PostIndexer.IndexPost(app.Services);
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("Healthy ✅"))
    .WithName("feed-health")
    .AllowAnonymous();

var feedGroup = app.MapGroup("/api/feed").RequireAuthorization();
const long MaxAttachmentSizeBytes = 20 * 1024 * 1024; // 20 MB

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

feedGroup.MapPost("", async (HttpContext http, [FromBody] CreatePost.Command command, IMediator mediator) =>
    {
        if (!http.User.TryGetAccountId(out var callerId) || callerId != command.Creator.Id)
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            return Results.BadRequest("Post description is required.");
        }

        var result = await mediator.Send(command);
        return result.Match(
            value => Results.Created($"/api/feed/{value}", value),
            Results.InternalServerError
        );
    })
    .WithName("create-post");

feedGroup.MapPost("/attachments", async (
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

    if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Only image attachments are supported.");
    }

    await using var input = file.OpenReadStream();
    var storedFile = await fileStorage.StoreFileAsync(
        bucket: "feed-attachments",
        originalFileName: file.FileName,
        contentType: file.ContentType,
        content: input,
        sizeBytes: file.Length,
        cancellationToken: http.RequestAborted
    );

    var response = new FeedAttachmentUploadResponse(
        storedFile.AttachmentId,
        storedFile.OriginalFileName,
        storedFile.ContentType,
        storedFile.SizeBytes,
        $"/feed/attachments/{storedFile.AttachmentId}"
    );

    return Results.Ok(response);
})
.DisableAntiforgery();

feedGroup.MapGet("/attachments/{attachmentId:guid}", async (
    HttpContext http,
    [FromRoute] Guid attachmentId,
    IAttachmentFileStorage fileStorage
) =>
{
    if (!http.User.TryGetAccountId(out _))
    {
        return Results.Unauthorized();
    }

    var attachment = await fileStorage.GetFileByPrefixAsync(
        bucket: "feed-attachments",
        fileNamePrefix: attachmentId.ToString(),
        cancellationToken: http.RequestAborted
    );
    if (attachment is null)
    {
        return Results.NotFound();
    }

    return Results.File(attachment.ContentStream, attachment.ContentType, enableRangeProcessing: true);
})
.WithName("get-feed-attachment");

feedGroup.MapGet("{id:guid}/details", async (Guid id, IMediator mediator) =>
    {
        var result = await mediator.Send(new GetPostDetails.Query(id));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("get-post-details");

feedGroup.MapPut("{id:guid}/reactions", async (HttpContext http, Guid id, [FromBody] ToggleReactionRequest request, IMediator mediator) =>
    {
        if (!http.User.TryGetAccountId(out var callerId) || callerId != request.Actor.Id)
        {
            return Results.Forbid();
        }

        var result = await mediator.Send(new TogglePostReaction.Command(id, request.Actor, request.ReactionType));
        return result.Match(
            _ => Results.NoContent(),
            _ => Results.NotFound()
        );
    })
    .WithName("toggle-post-reaction");

feedGroup.MapPost("{postId:guid}/comments", async (HttpContext http, Guid postId, [FromBody] AddCommentRequest request, IMediator mediator) =>
    {
        if (!http.User.TryGetAccountId(out var callerId) || callerId != request.Creator.Id)
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest("Comment content is required.");
        }

        var result = await mediator.Send(new AddComment.Command(postId, request.Creator, request.Content));
        return result.Match(
            value => Results.Created($"/api/feed/{postId}/comments/{value}", value),
            Results.NotFound
        );
    })
    .WithName("add-comment");

feedGroup.MapPost("{postId:guid}/comments/{commentId:guid}/replies", async (HttpContext http, Guid postId, Guid commentId, [FromBody] AddCommentRequest request, IMediator mediator) =>
    {
        if (!http.User.TryGetAccountId(out var callerId) || callerId != request.Creator.Id)
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest("Reply content is required.");
        }

        var result = await mediator.Send(new AddReply.Command(postId, commentId, request.Creator, request.Content));
        return result.Match(
            value => Results.Created($"/api/feed/{postId}/comments/{commentId}/replies/{value}", value),
            _ => Results.NotFound()
        );
    })
    .WithName("add-reply");

feedGroup.MapPut("comments/{commentId:guid}/reactions", async (HttpContext http, Guid commentId, [FromBody] ToggleReactionRequest request, IMediator mediator) =>
    {
        if (!http.User.TryGetAccountId(out var callerId) || callerId != request.Actor.Id)
        {
            return Results.Forbid();
        }

        var result = await mediator.Send(new ToggleCommentReaction.Command(commentId, request.Actor, request.ReactionType));
        return result.Match(
            _ => Results.NoContent(),
            _ => Results.NotFound()
        );
    })
    .WithName("toggle-comment-reaction");


app.Run();

public sealed record ToggleReactionRequest(Participant Actor, ReactionType ReactionType);
public sealed record AddCommentRequest(Participant Creator, string Content);
internal sealed record FeedAttachmentUploadResponse(
    string Id,
    string FileName,
    string MimeType,
    long SizeBytes,
    string Url
);
