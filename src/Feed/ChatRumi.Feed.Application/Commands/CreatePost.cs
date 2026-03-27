using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Feed.Domain.Aggregates;
using ChatRumi.Feed.Domain.ValueObject;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Commands;

public static class CreatePost
{
    public sealed record Command(
        Participant Creator,
        string Title,
        string Description
    ) : Mediator.IRequest<ErrorOr<string>>;

    public sealed class Handler(
        IElasticClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, ErrorOr<string>>
    {
        public async ValueTask<ErrorOr<string>> Handle(Command request, CancellationToken cancellationToken)
        {
            var post = Post.Create(request.Creator, request.Title, request.Description, [])
                .ToDocument();

            var response = await client.CreateDocumentAsync(
                post,
                cancellationToken
            );

            if (response.IsValid)
            {
                logger.LogInformation("Post was indexed successfully {Title}", request.Title);
                return post.Id.ToString();
            }

            logger.LogError("Post index failed. {Error}", response.OriginalException.StackTrace);
            return Error.Unexpected("Post creation failed.");
        }
    }
}