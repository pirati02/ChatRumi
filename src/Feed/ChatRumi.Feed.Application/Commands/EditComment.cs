using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Kernel.Results;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Commands;

public static class EditComment
{
    public sealed record Command(
        Guid CommentId,
        Guid ActorId,
        string Content
    ) : Mediator.IRequest<Response<Success>>;

    public sealed class Handler(
        IElasticClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Response<Success>>
    {
        public async ValueTask<Response<Success>> Handle(Command request, CancellationToken cancellationToken)
        {
            var commentResponse = await client.GetAsync<CommentDocument>(
                request.CommentId,
                g => g.Index(PostIndexes.Comments),
                cancellationToken);

            if (!commentResponse.Found || commentResponse.Source is null || commentResponse.Source.IsDeleted)
            {
                return ResponseError.NotFound("Comment not found.");
            }

            var comment = commentResponse.Source;
            if (comment.Creator.Id != request.ActorId)
            {
                return ResponseError.Forbidden("feed.comment.edit_forbidden", "Only comment owner can edit this comment.");
            }

            comment.Content = request.Content.Trim();
            comment.LastEditedAt = DateTimeOffset.UtcNow;

            var updateResponse = await client.UpdateAsync<CommentDocument>(
                request.CommentId,
                u => u.Index(PostIndexes.Comments).Doc(comment).Refresh(Elasticsearch.Net.Refresh.WaitFor),
                cancellationToken);

            if (!updateResponse.IsValid)
            {
                logger.LogError("Comment edit failed for comment {CommentId}. {Error}", request.CommentId, updateResponse.OriginalException?.Message);
                return ResponseError.Unexpected("Comment edit failed.");
            }

            return new Success();
        }
    }
}
