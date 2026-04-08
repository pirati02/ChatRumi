using ChatRumi.Feed.Application.Dtos;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Commands;

public static class EditPost
{
    public sealed record Command(
        Guid PostId,
        Guid ActorId,
        string Description
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

            if (!postResponse.Found || postResponse.Source is null || postResponse.Source.IsDeleted)
            {
                return Error.NotFound("Post not found.");
            }

            var post = postResponse.Source;
            if (post.Creator.Id != request.ActorId)
            {
                return Error.Forbidden("feed.post.edit_forbidden", "Only post owner can edit this post.");
            }

            post.Description = request.Description.Trim();
            post.LastEditedAt = DateTimeOffset.UtcNow;

            var updateResponse = await client.UpdateAsync<PostDocument>(
                request.PostId,
                u => u.Index(PostIndexes.Posts).Doc(post).Refresh(Elasticsearch.Net.Refresh.WaitFor),
                cancellationToken);

            if (!updateResponse.IsValid)
            {
                logger.LogError("Post edit failed for post {PostId}. {Error}", request.PostId, updateResponse.OriginalException?.Message);
                return Error.Unexpected("Post edit failed.");
            }

            return new Updated();
        }
    }
}
