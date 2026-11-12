using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Kernel.Results;
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
    ) : Mediator.IRequest<Response<Success>>;

    public sealed class Handler(
        IElasticClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Response<Success>>
    {
        public async ValueTask<Response<Success>> Handle(Command request, CancellationToken cancellationToken)
        {
            var postResponse = await client.GetAsync<PostDocument>(
                request.PostId,
                g => g.Index(PostIndexes.Posts),
                cancellationToken);

            if (!postResponse.Found || postResponse.Source is null || postResponse.Source.IsDeleted)
            {
                return ResponseError.NotFound("Post not found.");
            }

            var post = postResponse.Source;
            if (post.Creator.Id != request.ActorId)
            {
                return ResponseError.Forbidden("feed.post.edit_forbidden", "Only post owner can edit this post.");
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
                return ResponseError.Unexpected("Post edit failed.");
            }

            return new Success();
        }
    }
}
