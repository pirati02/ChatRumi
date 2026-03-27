using ChatRumi.Feed.Application.Dtos;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Queries;

public static class GetPost
{
    public sealed record Query(Guid Id) : Mediator.IRequest<ErrorOr<PostDocument>>;

    public class Handler(
        IElasticClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Query, ErrorOr<PostDocument>>
    {
        public async ValueTask<ErrorOr<PostDocument>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = await client.GetAsync<PostDocument>(request.Id, g => g.Index(PostIndexes.Posts), cancellationToken);

            if (response.IsValid)
            {
                logger.LogInformation("Post was indexed successfully {Title}", response.Source!.Title);
                return response.Source!;
            }

            logger.LogError("Post index failed. {Error}", response.OriginalException.StackTrace);
            return Error.NotFound("Post not found.");
        }
    }
}