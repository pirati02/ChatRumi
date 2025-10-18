using ChatRumi.Feed.Application;
using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Feed.Domain.ValueObject;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace ChatRumi.Feed.Infrastructure;

public static class PostIndexer
{
    public static async Task IndexPost(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IElasticClient>();
        const string indexName = PostIndexes.Posts;

        var exists = await client.Indices.ExistsAsync(indexName);
        if (!exists.Exists)
        {
            var createResponse = await client.Indices.CreateAsync(indexName, c => c
                .Map<PostDocument>(m => m
                    .AutoMap()
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
                )
            );

            if (!createResponse.IsValid)
                throw new Exception($"Failed to create index: {createResponse.DebugInformation}");
        }
    }
}