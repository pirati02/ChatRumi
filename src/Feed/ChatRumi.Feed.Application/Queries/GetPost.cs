using ChatRumi.Feed.Application.Dtos;
using Elastic.Clients.Elasticsearch;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatRumi.Feed.Application.Queries;

public static class GetPost
{
    public sealed record Query(Guid Id) : IRequest<ErrorOr<PostDocument>>;

    public class Handler(
        ElasticsearchClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Query, ErrorOr<PostDocument>>
    {
        public async Task<ErrorOr<PostDocument>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = await client.GetAsync<PostDocument>(request.Id, g => g.Index("posts"), cancellationToken);

            if (response.IsValidResponse)
            {
                logger.LogInformation("Post was indexed successfully {Title}", response.Source!.Title);
                return response.Source!;
            }

            logger.LogError("Post index failed. {Error}", response.ElasticsearchServerError?.Error.Reason);
            return Error.NotFound("Post not found.");
        }
    }
}