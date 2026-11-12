using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Kernel.Results;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Queries;

public static class GetPost
{
    public sealed record Query(Guid Id) : Mediator.IRequest<Response<PostDocument>>;

    public class Handler(
        IElasticClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Query, Response<PostDocument>>
    {
        public async ValueTask<Response<PostDocument>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = await client.GetAsync<PostDocument>(request.Id, g => g.Index(PostIndexes.Posts), cancellationToken);

            if (!response.Found || response.Source is null || response.Source.IsDeleted)
            {
                return ResponseError.NotFound("Post not found.");
            }

            if (response.IsValid)
            {
                logger.LogInformation("Post was loaded successfully {PostId}", response.Source.Id);
                return response.Source;
            }

            logger.LogError("Post load failed for post {PostId}. {Error}", request.Id, response.OriginalException?.Message);
            return ResponseError.NotFound("Post not found.");
        }
    }
}