using ChatRumi.Chat.Application.Dto.Request;
using Marten;

namespace ChatRumi.Chat.Application.Projections.ExistingChat;

// ReSharper disable once ClassNeverInstantiated.Global
public static class ExistingChatProjectionExtensions
{
    public static Task<ExistingChatProjection?> TryGetExistingChat(
        this IDocumentSession session,
        ParticipantDto[] participants,
        CancellationToken cancellationToken
    )
    {
        var participantHash = string.Join("|", participants.OrderBy(a => a.Id)
            .Select(a => a.Id.ToString("N")));

        return session.Query<ExistingChatProjection>()
            .FirstOrDefaultAsync(
                p =>
                    p.ParticipantsHash == participantHash,
                token: cancellationToken
            );
    }
}