using ChatRumi.Feed.Application.Dtos;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Commands;

public static class DeletePost
{
    public sealed record Command(
        Guid PostId,
        Guid ActorId
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
                return Error.Forbidden("feed.post.delete_forbidden", "Only post owner can delete this post.");
            }

            post.IsDeleted = true;
            post.DeletedAt = DateTimeOffset.UtcNow;
            post.LastEditedAt = post.DeletedAt;
            post.Description = string.Empty;
            post.Attachments = [];
            post.Reactions = [];
            post.Shares = [];

            var updateResponse = await client.UpdateAsync<PostDocument>(
                request.PostId,
                u => u.Index(PostIndexes.Posts).Doc(post).Refresh(Elasticsearch.Net.Refresh.WaitFor),
                cancellationToken);

            if (!updateResponse.IsValid)
            {
                logger.LogError("Post delete failed for post {PostId}. {Error}", request.PostId, updateResponse.OriginalException?.Message);
                return Error.Unexpected("Post delete failed.");
            }

            return new Updated();
        }
    }
}
