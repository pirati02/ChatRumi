using ChatRumi.Feed.Application.Dtos;
using ChatRum.InterCommunication;
using ChatRumi.Feed.Application.IntegrationEvents;
using ChatRumi.Feed.Domain.ValueObject;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Commands;

public static class TogglePostReaction
{
    public sealed record Command(
        Guid PostId,
        Participant Actor,
        ReactionType ReactionType
    ) : Mediator.IRequest<ErrorOr<Updated>>;

    public sealed class Handler(
        IElasticClient client,
        IDispatcher dispatcher,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, ErrorOr<Updated>>
    {
        public async ValueTask<ErrorOr<Updated>> Handle(Command request, CancellationToken cancellationToken)
        {
            var postResponse = await client.GetAsync<PostDocument>(
                request.PostId,
                g => g.Index(PostIndexes.Posts),
                cancellationToken);

            if (!postResponse.Found || postResponse.Source is null)
            {
                return Error.NotFound("Post not found.");
            }

            var post = postResponse.Source;
            var existingReaction = post.Reactions.FirstOrDefault(x => x.Actor.Id == request.Actor.Id);
            var shouldPublishNotification = FeedNotificationRules.ShouldNotifyForReaction(
                existingReaction,
                request.ReactionType,
                post.Creator.Id,
                request.Actor.Id
            );
            post.Reactions.ToggleSingleReaction(request.Actor, request.ReactionType);

            var updateResponse = await client.UpdateAsync<PostDocument>(
                request.PostId,
                u => u.Index(PostIndexes.Posts).Doc(post).Refresh(Elasticsearch.Net.Refresh.WaitFor),
                cancellationToken);

            if (!updateResponse.IsValid)
            {
                logger.LogError("Post reaction update failed for post {PostId}. {Error}", request.PostId, updateResponse.OriginalException?.Message);
                return Error.Unexpected("Post reaction update failed.");
            }

            if (shouldPublishNotification)
            {
                await dispatcher.ProduceAsync(
                    Topics.NotificationTriggeredTopic,
                    post.Creator.Id.ToString(),
                    new NotificationTriggered(
                        post.Creator.Id,
                        request.Actor.Id,
                        request.Actor.FirstName,
                        request.Actor.LastName,
                        request.Actor.NickName,
                        "PostReaction",
                        request.PostId,
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
