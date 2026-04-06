using ChatRumi.Notification.Application;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace ChatRumi.Notification.Infrastructure;

public static class NotificationIndexer
{
    public static async Task EnsureIndex(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IElasticClient>();
        const string indexName = NotificationIndexes.Notifications;

        var exists = await client.Indices.ExistsAsync(indexName);
        if (exists.Exists)
        {
            return;
        }

        var createResponse = await client.Indices.CreateAsync(indexName, c => c
            .Map<NotificationDocument>(m => m
                .AutoMap()
                .Dynamic()
                .Properties(ps => ps
                    .Keyword(k => k.Name(n => n.RecipientId))
                    .Keyword(k => k.Name(n => n.ActorId))
                    .Keyword(k => k.Name(n => n.Type))
                    .Keyword(k => k.Name(n => n.TargetId))
                    .Keyword(k => k.Name(n => n.Reaction))
                    .Boolean(b => b.Name(n => n.IsRead))
                    .Date(d => d.Name(n => n.CreatedAt))
                    .Date(d => d.Name(n => n.ReadAt))
                )
            ).Aliases(a => a.Alias("user-notifications").Alias("feed-notifications"))
        );

        if (!createResponse.IsValid)
        {
            throw new Exception($"Failed to create notifications index: {createResponse.DebugInformation}");
        }
    }
}
