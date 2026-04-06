using ChatRumi.Feed.Domain.ValueObject;

namespace ChatRumi.Feed.Application.Dtos;

public static class ReactionToggleExtensions
{
    public static void ToggleSingleReaction(this List<Reaction> reactions, Participant actor, ReactionType reactionType)
    {
        var existingReaction = reactions.FirstOrDefault(r => r.Actor.Id == actor.Id);
        if (existingReaction is null)
        {
            reactions.Add(new Reaction
            {
                Actor = actor,
                ReactionType = reactionType
            });

            return;
        }

        if (existingReaction.ReactionType == reactionType)
        {
            reactions.Remove(existingReaction);
            return;
        }

        existingReaction.ReactionType = reactionType;
    }
}
