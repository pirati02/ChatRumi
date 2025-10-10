using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Feed.Domain.Aggregates;
using ChatRumi.Feed.Domain.ValueObject;
using Elastic.Clients.Elasticsearch;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatRumi.Feed.Application.Commands;

public static class CreatePost
{
    public sealed record Command(
        Participant Creator,
        string Title,
        string Description,
        IEnumerable<Attachment> Attachments
    ) : IRequest<ErrorOr<string>>;

    public sealed class Handler(
        ElasticsearchClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, ErrorOr<string>>
    {
        public async Task<ErrorOr<string>> Handle(Command request, CancellationToken cancellationToken)
        {
            var post = Post.Create(request.Creator, request.Title, request.Description, request.Attachments)
                .ToDocument();

            var response = await client.IndexAsync(
                post,
                index => index.Index(PostIndexes.Posts)
                    .Id(Guid.NewGuid().ToString()),
                cancellationToken
            );

            if (response.IsValidResponse)
            {
                logger.LogInformation("Post was indexed successfully {Title}", request.Title);
                return post.PostId.ToString();
            }

            logger.LogError("Post index failed. {Error}", response.ElasticsearchServerError?.Error.Reason);
            return Error.Unexpected("Post creation failed.");
        }
    }
}