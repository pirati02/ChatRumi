using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Feed.Domain.Aggregates;
using ChatRumi.Feed.Domain.ValueObject;
using ChatRumi.Kernel.Results;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Commands;

public static class CreatePost
{
    public sealed record Command(
        Participant Creator,
        string Description,
        IReadOnlyCollection<Guid>? AttachmentIds
    ) : Mediator.IRequest<Response<string>>;

    public sealed class Handler(
        IElasticClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Response<string>>
    {
        public async ValueTask<Response<string>> Handle(Command request, CancellationToken cancellationToken)
        {
            var attachments = request.AttachmentIds?
                .Distinct()
                .Select(id => new ChatRumi.Feed.Domain.ValueObject.Attachment { Id = new AttachmentId(id) })
                .ToList() ?? [];

            var post = Post.Create(request.Creator, request.Description, attachments)
                .ToDocument();

            var response = await client.CreateDocumentAsync(
                post,
                cancellationToken
            );

            if (response.IsValid)
            {
                logger.LogInformation("Post was indexed successfully {PostId}", post.Id);
                return post.Id.ToString();
            }

            logger.LogError("Post index failed. {Error}", response.OriginalException?.StackTrace);
            return ResponseError.Unexpected("Post creation failed.");
        }
    }
}