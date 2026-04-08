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

            if (!response.Found || response.Source is null || response.Source.IsDeleted)
            {
                return Error.NotFound("Post not found.");
            }

            if (response.IsValid)
            {
                logger.LogInformation("Post was loaded successfully {PostId}", response.Source.Id);
                return response.Source;
            }

            logger.LogError("Post load failed for post {PostId}. {Error}", request.Id, response.OriginalException?.Message);
            return Error.NotFound("Post not found.");
        }
    }
}