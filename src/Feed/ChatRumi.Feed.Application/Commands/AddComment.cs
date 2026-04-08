using ChatRumi.Feed.Application.Dtos;
using ChatRum.InterCommunication;
using ChatRumi.Feed.Application.IntegrationEvents;
using ChatRumi.Feed.Domain.ValueObject;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Commands;

public static class AddComment
{
    public sealed record Command(
        Guid PostId,
        Participant Creator,
        string Content
    ) : Mediator.IRequest<ErrorOr<Guid>>;

    public sealed class Handler(
        IElasticClient client,
        IDispatcher dispatcher,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, ErrorOr<Guid>>
    {
        public async ValueTask<ErrorOr<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var postResponse = await client.GetAsync<PostDocument>(
                request.PostId,
                g => g.Index(PostIndexes.Posts),
                cancellationToken);

            if (!postResponse.Found || postResponse.Source is null)
            {
                return Error.NotFound("Post not found.");
            }

            var comment = new CommentDocument
            {
                PostId = request.PostId,
                Creator = request.Creator,
                Content = request.Content.Trim()
            };

            var response = await client.IndexAsync(
                comment,
                i => i.Index(PostIndexes.Comments).Id(comment.Id).Refresh(Elasticsearch.Net.Refresh.WaitFor),
                cancellationToken);

            if (!response.IsValid)
            {
                logger.LogError("Comment creation failed for post {PostId}. {Error}", request.PostId, response.OriginalException?.Message);
                return Error.Unexpected("Comment creation failed.");
            }

            if (FeedNotificationRules.ShouldNotify(postResponse.Source.Creator.Id, request.Creator.Id))
            {
                await dispatcher.ProduceAsync(
                    Topics.NotificationTriggeredTopic,
                    postResponse.Source.Creator.Id.ToString(),
                    new NotificationTriggered(
                        postResponse.Source.Creator.Id,
                        request.Creator.Id,
                        request.Creator.FirstName,
                        request.Creator.LastName,
                        request.Creator.NickName,
                        "PostComment",
                        request.PostId,
                        request.Content.Trim(),
                        null,
                        DateTimeOffset.UtcNow
                    ),
                    cancellationToken
                );
            }

            return comment.Id;
        }
    }
}
