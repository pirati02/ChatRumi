using ChatRumi.Feed.Domain.ValueObject;

namespace ChatRumi.Feed.Application.IntegrationEvents;

public static class FeedNotificationRules
{
    public static bool ShouldNotify(Guid recipientId, Guid actorId)
    {
        return recipientId != actorId;
    }

    public static bool ShouldNotifyForReaction(Reaction? existingReaction, ReactionType requestedReactionType, Guid recipientId, Guid actorId)
    {
        if (!ShouldNotify(recipientId, actorId))
        {
            return false;
        }

        return existingReaction is null || existingReaction.ReactionType != requestedReactionType;
    }
}
