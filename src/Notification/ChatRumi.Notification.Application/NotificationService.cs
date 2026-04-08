using Nest;

namespace ChatRumi.Notification.Application;

public sealed class NotificationService(
    IElasticClient client,
    INotificationRealtimePublisher realtimePublisher
) : INotificationService
{
    public async Task<NotificationListItem?> CreateFromEventAsync(NotificationTriggered notificationTriggered, CancellationToken cancellationToken)
    {
        if (notificationTriggered.RecipientId == notificationTriggered.ActorId)
        {
            return null;
        }

        var document = new NotificationDocument
        {
            RecipientId = notificationTriggered.RecipientId,
            ActorId = notificationTriggered.ActorId,
            ActorDisplayName = BuildDisplayName(notificationTriggered.ActorFirstName, notificationTriggered.ActorLastName, notificationTriggered.ActorNickName, notificationTriggered.ActorId),
            Type = notificationTriggered.Type,
            TargetId = notificationTriggered.TargetId,
            TargetPreview = notificationTriggered.TargetPreview,
            Reaction = notificationTriggered.Reaction,
            CreatedAt = notificationTriggered.CreatedAt
        };

        var indexResponse = await client.IndexAsync(
            document,
            i => i.Index(NotificationIndexes.Notifications).Id(document.Id).Refresh(Elasticsearch.Net.Refresh.WaitFor),
            cancellationToken
        );

        if (!indexResponse.IsValid)
        {
            return null;
        }

        var listItem = ToListItem(document);
        await realtimePublisher.NotifyCreatedAsync(listItem, cancellationToken);
        var unreadCount = await GetUnreadCountAsync(document.RecipientId, cancellationToken);
        await realtimePublisher.NotifyUnreadCountChangedAsync(document.RecipientId, unreadCount, cancellationToken);
        return listItem;
    }

    public async Task<NotificationPage> GetPageAsync(Guid recipientId, DateTimeOffset? cursor, int pageSize, CancellationToken cancellationToken)
    {
        var normalizedSize = Math.Clamp(pageSize, 1, 50);
        var response = await client.SearchAsync<NotificationDocument>(
            s =>
            {
                s = s
                    .Index(NotificationIndexes.Notifications)
                    .Sort(ss => ss.Descending(x => x.CreatedAt).Descending(x => x.Id))
                    .Size(normalizedSize + 1);

                if (cursor.HasValue)
                {
                    return s.Query(q => q.Bool(b => b.Filter(
                        f => f.Term(t => t.Field(x => x.RecipientId).Value(recipientId)),
                        f => f.DateRange(r => r.Field(x => x.CreatedAt).LessThan(DateMath.Anchored(cursor.Value.UtcDateTime)))
                    )));
                }

                return s.Query(q => q.Term(t => t.Field(x => x.RecipientId).Value(recipientId)));
            },
            cancellationToken
        );

        if (!response.IsValid)
        {
            return new NotificationPage([], null);
        }

        var ordered = response.Documents
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToList();

        var items = ordered.Take(normalizedSize).Select(ToListItem).ToList();
        DateTimeOffset? nextCursor = ordered.Count > normalizedSize ? ordered[normalizedSize].CreatedAt : null;
        return new NotificationPage(items, nextCursor);
    }

    public async Task<long> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken)
    {
        var countResponse = await client.CountAsync<NotificationDocument>(
            c => c.Index(NotificationIndexes.Notifications).Query(q => q.Bool(b => b.Filter(
                f => f.Term(t => t.Field(x => x.RecipientId).Value(recipientId)),
                f => f.Term(t => t.Field(x => x.IsRead).Value(false))
            ))),
            cancellationToken
        );

        return countResponse.IsValid ? countResponse.Count : 0;
    }

    public async Task<bool> MarkReadAsync(Guid recipientId, Guid notificationId, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync<NotificationDocument>(
            notificationId,
            g => g.Index(NotificationIndexes.Notifications),
            cancellationToken
        );

        if (!response.Found || response.Source is null || response.Source.RecipientId != recipientId)
        {
            return false;
        }

        if (!response.Source.IsRead)
        {
            response.Source.IsRead = true;
            response.Source.ReadAt = DateTimeOffset.UtcNow;
            var updateResponse = await client.UpdateAsync<NotificationDocument>(
                notificationId,
                u => u.Index(NotificationIndexes.Notifications).Doc(response.Source).Refresh(Elasticsearch.Net.Refresh.WaitFor),
                cancellationToken
            );

            if (!updateResponse.IsValid)
            {
                return false;
            }
        }

        var unreadCount = await GetUnreadCountAsync(recipientId, cancellationToken);
        await realtimePublisher.NotifyUnreadCountChangedAsync(recipientId, unreadCount, cancellationToken);
        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid recipientId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var updateByQueryResponse = await client.UpdateByQueryAsync<NotificationDocument>(
            u => u
                .Index(NotificationIndexes.Notifications)
                .Query(q => q.Bool(b => b.Filter(
                    f => f.Term(t => t.Field(x => x.RecipientId).Value(recipientId)),
                    f => f.Term(t => t.Field(x => x.IsRead).Value(false))
                )))
                .Script(s => s.Source("ctx._source.isRead = true; ctx._source.readAt = params.readAt;").Params(p => p.Add("readAt", now)))
                .Refresh(true),
            cancellationToken
        );

        if (!updateByQueryResponse.IsValid)
        {
            return 0;
        }

        await realtimePublisher.NotifyUnreadCountChangedAsync(recipientId, 0, cancellationToken);
        return (int)updateByQueryResponse.Updated;
    }

    private static NotificationListItem ToListItem(NotificationDocument document)
    {
        return new NotificationListItem(
            document.Id,
            document.RecipientId,
            document.ActorId,
            document.ActorDisplayName,
            document.ActorAvatarUrl,
            document.Type,
            document.TargetId,
            document.TargetPreview,
            document.Reaction,
            document.CreatedAt,
            document.IsRead
        );
    }

    private static string BuildDisplayName(string firstName, string lastName, string? nickName, Guid actorId)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        if (!string.IsNullOrWhiteSpace(nickName))
        {
            return nickName;
        }

        return actorId.ToString();
    }
}
