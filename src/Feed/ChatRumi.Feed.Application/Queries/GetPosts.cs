using ChatRumi.Feed.Application.Dtos;
using MediatR;
using Nest;

namespace ChatRumi.Feed.Application.Queries;

public static class GetPosts
{
    public sealed record Query(int Size = 10) : MediatR.IRequest<IEnumerable<PostDocument>>;

    public class Handler(
        IElasticClient client
    ) : IRequestHandler<Query, IEnumerable<PostDocument>>
    {
        public async Task<IEnumerable<PostDocument>> Handle(Query request, CancellationToken cancellationToken)
        {
            var seed = DateTime.UtcNow.Ticks;

            var response = await client.SearchAsync<PostDocument>(s => s
                .Size(request.Size)
                .Query(q => q
                    .FunctionScore(fs => fs
                        .Functions(f => f
                            .RandomScore(rs => rs.Seed(seed))
                        )
                    )
                ), cancellationToken);

            return response.IsValid ? response.Documents : [];
        }
    }
}