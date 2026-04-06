using ChatRumi.Feed.Application.IntegrationEvents;
using ChatRumi.Feed.Domain.ValueObject;
using Xunit;

namespace ChatRumi.Feed.Application.Tests;

public class FeedNotificationRulesTests
{
    [Fact]
    public void ShouldNotify_ReturnsFalse_ForSelfAction()
    {
        var actorId = Guid.NewGuid();
        var shouldNotify = FeedNotificationRules.ShouldNotify(actorId, actorId);
        Assert.False(shouldNotify);
    }

    [Fact]
    public void ShouldNotify_ReturnsTrue_ForDifferentUsers()
    {
        var shouldNotify = FeedNotificationRules.ShouldNotify(Guid.NewGuid(), Guid.NewGuid());
        Assert.True(shouldNotify);
    }

    [Fact]
    public void ShouldNotifyForReaction_ReturnsFalse_WhenReactionIsRemoved()
    {
        var existing = new Reaction
        {
            Actor = new Participant
            {
                Id = Guid.NewGuid(),
                FirstName = "A",
                LastName = "B"
            },
            ReactionType = ReactionType.Like
        };

        var shouldNotify = FeedNotificationRules.ShouldNotifyForReaction(existing, ReactionType.Like, Guid.NewGuid(), Guid.NewGuid());
        Assert.False(shouldNotify);
    }

    [Fact]
    public void ShouldNotifyForReaction_ReturnsTrue_WhenReactionIsAddedOrChanged()
    {
        var shouldNotifyWhenAdded = FeedNotificationRules.ShouldNotifyForReaction(null, ReactionType.Heart, Guid.NewGuid(), Guid.NewGuid());
        var existing = new Reaction
        {
            Actor = new Participant
            {
                Id = Guid.NewGuid(),
                FirstName = "A",
                LastName = "B"
            },
            ReactionType = ReactionType.Like
        };

        var shouldNotifyWhenChanged = FeedNotificationRules.ShouldNotifyForReaction(existing, ReactionType.Heart, Guid.NewGuid(), Guid.NewGuid());
        Assert.True(shouldNotifyWhenAdded);
        Assert.True(shouldNotifyWhenChanged);
    }
}
