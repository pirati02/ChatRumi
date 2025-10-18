using ChatRumi.Feed.Application.Dtos;
using MediatR;
using Nest;

namespace ChatRumi.Feed.Application.Queries;

public static class GetPosts
{
    public sealed record Query(Guid CreatorId, int Size = 10) : MediatR.IRequest<IEnumerable<PostDocument>>;

    public class Handler(
        IElasticClient client
    ) : IRequestHandler<Query, IEnumerable<PostDocument>>
    {
        public async Task<IEnumerable<PostDocument>> Handle(Query request, CancellationToken cancellationToken)
        {
            var seed = DateTime.UtcNow.Ticks;

            var randomPosts = await client.SearchAsync<PostDocument>(s => s
                .Size(request.Size)
                .Query(q => q
                    .Bool(b => b
                        .MustNot(mn => mn.Term(t => t.Field(f => f.Creator.Id).Value(request.CreatorId)))
                        .Must(mu => mu.FunctionScore(fs => fs
                            .Query(qr => qr.MatchAll())
                            .Functions(f => f.RandomScore(rs => rs.Seed(seed)))
                        ))
                    )
                ), cancellationToken);

            var myPosts = await client.SearchAsync<PostDocument>(s => s
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.Creator.Id)
                        .Value(request.CreatorId)
                    )
                )
                .Size(request.Size), cancellationToken);

            if (!randomPosts.IsValid || !myPosts.IsValid)
                return [];

            var combined = myPosts.Documents
                .Concat(randomPosts.Documents)
                .DistinctBy(p => p.Id)
                .ToList();

            return combined;
        }
    }
}