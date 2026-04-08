using ChatRumi.Feed.Application.Dtos;
using ChatRum.InterCommunication;
using ChatRumi.Feed.Application.IntegrationEvents;
using ChatRumi.Feed.Domain.ValueObject;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Commands;

public static class ToggleCommentReaction
{
    public sealed record Command(
        Guid CommentId,
        Participant Actor,
        ReactionType ReactionType
    ) : Mediator.IRequest<ErrorOr<Updated>>;

    public sealed class Handler(
        IElasticClient client,
        IOutboxWriter outboxWriter,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, ErrorOr<Updated>>
    {
        public async ValueTask<ErrorOr<Updated>> Handle(Command request, CancellationToken cancellationToken)
        {
            var commentResponse = await client.GetAsync<CommentDocument>(
                request.CommentId,
                g => g.Index(PostIndexes.Comments),
                cancellationToken);

            if (!commentResponse.Found || commentResponse.Source is null)
            {
                return Error.NotFound("Comment not found.");
            }

            var comment = commentResponse.Source;
            var existingReaction = comment.Reactions.FirstOrDefault(x => x.Actor.Id == request.Actor.Id);
            var shouldPublishNotification = FeedNotificationRules.ShouldNotifyForReaction(
                existingReaction,
                request.ReactionType,
                comment.Creator.Id,
                request.Actor.Id
            );
            comment.Reactions.ToggleSingleReaction(request.Actor, request.ReactionType);

            var updateResponse = await client.UpdateAsync<CommentDocument>(
                request.CommentId,
                u => u.Index(PostIndexes.Comments).Doc(comment).Refresh(Elasticsearch.Net.Refresh.WaitFor),
                cancellationToken);

            if (!updateResponse.IsValid)
            {
                logger.LogError("Comment reaction update failed for comment {CommentId}. {Error}", request.CommentId, updateResponse.OriginalException?.Message);
                return Error.Unexpected("Comment reaction update failed.");
            }

            if (shouldPublishNotification)
            {
                await outboxWriter.EnqueueAsync(
                    Topics.NotificationTriggeredTopic,
                    comment.Creator.Id.ToString(),
                    new NotificationTriggered(
                        comment.Creator.Id,
                        request.Actor.Id,
                        request.Actor.FirstName,
                        request.Actor.LastName,
                        request.Actor.NickName,
                        "CommentReaction",
                        request.CommentId,
                        null,
                        request.ReactionType.ToString(),
                        DateTimeOffset.UtcNow
                    ),
                    cancellationToken
                );
            }

            return new Updated();
        }
    }
}
