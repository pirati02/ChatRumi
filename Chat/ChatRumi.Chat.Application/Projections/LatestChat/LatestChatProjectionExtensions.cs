using Marten;

namespace ChatRumi.Chat.Application.Projections.LatestChat;

// ReSharper disable once ClassNeverInstantiated.Global
public static class LatestChatProjectionExtensions
{
    public static Task<IReadOnlyList<LatestChatProjection>> TryGetTop10LatestChatsAsync(
        this IDocumentSession session,
        Guid participant,
        CancellationToken cancellationToken
    )
    {
        return session.Query<LatestChatProjection>()
            .Where(p => p.Participants.Any(a => a.Id == participant))
            .ToListAsync(token: cancellationToken);
    }
}