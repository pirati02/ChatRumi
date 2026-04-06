using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Feed.Domain.ValueObject;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Commands;

public static class AddReply
{
    public sealed record Command(
        Guid PostId,
        Guid ParentCommentId,
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
            var parentCommentResponse = await client.GetAsync<CommentDocument>(
                request.ParentCommentId,
                g => g.Index(PostIndexes.Comments),
                cancellationToken);

            if (!parentCommentResponse.Found || parentCommentResponse.Source is null)
            {
                return Error.NotFound("Parent comment not found.");
            }

            if (parentCommentResponse.Source.PostId != request.PostId)
            {
                return Error.Validation("Reply post mismatch.");
            }

            var reply = new CommentDocument
            {
                PostId = request.PostId,
                ParentCommentId = request.ParentCommentId,
                Creator = request.Creator,
                Content = request.Content.Trim()
            };

            var response = await client.IndexAsync(
                reply,
                i => i.Index(PostIndexes.Comments).Id(reply.Id).Refresh(Elasticsearch.Net.Refresh.WaitFor),
                cancellationToken);

            if (!response.IsValid)
            {
                logger.LogError("Reply creation failed for comment {CommentId}. {Error}", request.ParentCommentId, response.OriginalException?.Message);
                return Error.Unexpected("Reply creation failed.");
            }

            return reply.Id;
        }
    }
}
