using ChatRumi.Feed.Application;
using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Feed.Application.Outbox;
using ChatRumi.Feed.Domain.ValueObject;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace ChatRumi.Feed.Infrastructure;

public static class PostIndexer
{
    public static async Task IndexPost(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IElasticClient>();
        await EnsurePostIndex(client);
        await EnsureCommentsIndex(client);
        await EnsureOutboxIndex(client);
    }

    private static async Task EnsurePostIndex(IElasticClient client)
    {
        const string indexName = PostIndexes.Posts;

        var exists = await client.Indices.ExistsAsync(indexName);
        if (!exists.Exists)
        {
            var createResponse = await client.Indices.CreateAsync(indexName, c => c
                .Map<PostDocument>(m => m
                    .AutoMap()
                    .Dynamic()
                    .Properties(ps => ps
                        .Object<Participant>(p => p
                            .Name(n => n.Creator)
                            .Properties(pp => pp
                                .Keyword(k => k.Name(nn => nn.Id))
                                .Text(t => t.Name(nn => nn.FirstName))
                                .Text(t => t.Name(nn => nn.LastName))
                                .Keyword(k => k.Name(nn => nn.NickName))
                            )
                        )
                        .Date(d => d.Name(n => n.CreationDate))
                    )
                ).Aliases(a => a.Alias("user-posts").Alias("feed-posts"))
            );

            if (!createResponse.IsValid)
                throw new Exception($"Failed to create index: {createResponse.DebugInformation}");
        }
    }

    private static async Task EnsureCommentsIndex(IElasticClient client)
    {
        const string indexName = PostIndexes.Comments;

        var exists = await client.Indices.ExistsAsync(indexName);
        if (!exists.Exists)
        {
            var createResponse = await client.Indices.CreateAsync(indexName, c => c
                .Map<CommentDocument>(m => m
                    .AutoMap()
                    .Dynamic()
                    .Properties(ps => ps
                        .Keyword(k => k.Name(n => n.PostId))
                        .Keyword(k => k.Name(n => n.ParentCommentId))
                        .Object<Participant>(p => p
                            .Name(n => n.Creator)
                            .Properties(pp => pp
                                .Keyword(k => k.Name(nn => nn.Id))
                                .Text(t => t.Name(nn => nn.FirstName))
                                .Text(t => t.Name(nn => nn.LastName))
                                .Keyword(k => k.Name(nn => nn.NickName))
                            )
                        )
                        .Date(d => d.Name(n => n.CreationDate))
                    )
                ).Aliases(a => a.Alias("feed-comments"))
            );

            if (!createResponse.IsValid)
                throw new Exception($"Failed to create comments index: {createResponse.DebugInformation}");
        }
    }

    private static async Task EnsureOutboxIndex(IElasticClient client)
    {
        const string indexName = PostIndexes.Outbox;

        var exists = await client.Indices.ExistsAsync(indexName);
        if (!exists.Exists)
        {
            var createResponse = await client.Indices.CreateAsync(indexName, c => c
                .Map<FeedOutboxMessage>(m => m
                    .AutoMap()
                    .Properties(ps => ps
                        .Date(d => d.Name(n => n.CreatedOnUtc))
                        .Date(d => d.Name(n => n.NextAttemptAtUtc))
                        .Date(d => d.Name(n => n.ProcessedOnUtc))
                        .Number(n => n.Name(nn => nn.AttemptCount).Type(NumberType.Integer))
                        .Keyword(k => k.Name(nn => nn.Topic))
                        .Keyword(k => k.Name(nn => nn.Key))
                        .Keyword(k => k.Name(nn => nn.EventType))
                    )
                )
            );

            if (!createResponse.IsValid)
                throw new Exception($"Failed to create outbox index: {createResponse.DebugInformation}");
        }
    }

}