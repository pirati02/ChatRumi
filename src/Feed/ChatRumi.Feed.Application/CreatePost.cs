using ChatRumi.Feed.Domain.Aggregates;
using ChatRumi.Feed.Domain.ValueObject;
using Elastic.Clients.Elasticsearch;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatRumi.Feed.Application;

public static class CreatePost
{
    public sealed record Command(
        Participant Creator,
        string Title,
        string Description,
        IEnumerable<Attachment> Attachments
    ) : IRequest<string>;

    public sealed class Handler(
        ElasticsearchClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, string>
    {
        public async Task<string> Handle(Command request, CancellationToken cancellationToken)
        {
            var post = Post.Create(request.Creator, request.Title, request.Description, request.Attachments);

            var postDocument = post.ToDocument();

            var response = await client.IndexAsync(postDocument, i => i
                .Index(PostIndexes.Posts)
                .Id(Guid.NewGuid().ToString()), cancellationToken);

            if (response.IsValidResponse)
            {
                logger.LogInformation("Post was indexed successfully {Title}", request.Title);
            }
            else
            {
                logger.LogError("Post index failed. {Error}", response.ElasticsearchServerError?.Error.Reason);   
            }
            
            return post.Id.ToString();
        }
    }
}