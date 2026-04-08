using ChatRumi.Feed.Application.Dtos;
using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Nest;

namespace ChatRumi.Feed.Application.Queries;

public static class GetPostDetails
{
    public sealed record Query(Guid Id) : Mediator.IRequest<ErrorOr<PostDetailsDocument>>;

    public sealed class Handler(
        IElasticClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Query, ErrorOr<PostDetailsDocument>>
    {
        public async ValueTask<ErrorOr<PostDetailsDocument>> Handle(Query request, CancellationToken cancellationToken)
        {
            var postResponse = await client.GetAsync<PostDocument>(request.Id, g => g.Index(PostIndexes.Posts), cancellationToken);
            if (!postResponse.Found || postResponse.Source is null || postResponse.Source.IsDeleted)
            {
                return Error.NotFound("Post not found.");
            }

            var commentsResponse = await client.SearchAsync<CommentDocument>(s => s
                    .Index(PostIndexes.Comments)
                    .Query(q => q.Bool(b => b
                        .Must(mu => mu.Term(t => t.Field(f => f.PostId).Value(request.Id)))
                        .MustNot(mn => mn.Term(t => t.Field(f => f.IsDeleted).Value(true)))
                    ))
                    .Sort(ss => ss.Ascending(c => c.CreationDate))
                    .Size(1000),
                cancellationToken);

            if (!commentsResponse.IsValid)
            {
                logger.LogError("Failed to load comments for post {PostId}. {Error}", request.Id, commentsResponse.OriginalException?.Message);
                return Error.Unexpected("Failed to load post details.");
            }

            var commentLookup = commentsResponse.Documents
                .ToDictionary(
                    comment => comment.Id,
                    comment => new CommentThreadDocument
                    {
                        Comment = comment,
                        Replies = []
                    });

            var roots = new List<CommentThreadDocument>();
            foreach (var thread in commentLookup.Values)
            {
                if (thread.Comment.ParentCommentId is { } parentId && commentLookup.TryGetValue(parentId, out var parent))
                {
                    parent.Replies.Add(thread);
                    continue;
                }

                roots.Add(thread);
            }

            return new PostDetailsDocument
            {
                Post = postResponse.Source,
                Comments = roots
            };
        }
    }
}
