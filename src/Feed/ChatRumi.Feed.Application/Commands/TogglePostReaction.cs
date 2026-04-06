using ChatRumi.Feed.Application.Dtos;
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

            return new Updated();
        }
    }
}
