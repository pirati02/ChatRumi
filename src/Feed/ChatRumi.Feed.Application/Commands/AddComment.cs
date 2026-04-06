using ChatRumi.Feed.Application.Dtos;
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
        ILogger<Handler> logger
    ) : IRequestHandler<Command, ErrorOr<Guid>>
    {
        public async ValueTask<ErrorOr<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var postResponse = await client.GetAsync<PostDocument>(
                request.PostId,
                g => g.Index(PostIndexes.Posts),
                cancellationToken);

            if (!postResponse.Found)
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

            return comment.Id;
        }
    }
}
