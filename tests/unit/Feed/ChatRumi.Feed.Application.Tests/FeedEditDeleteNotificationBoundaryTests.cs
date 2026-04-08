using ChatRum.InterCommunication;
using ChatRumi.Feed.Application.Commands;
using Xunit;

namespace ChatRumi.Feed.Application.Tests;

public class FeedEditDeleteNotificationBoundaryTests
{
    [Fact]
    public void EditAndDeleteHandlers_DoNotDependOnOutboxWriter()
    {
        var handlers = new[]
        {
            typeof(EditPost.Handler),
            typeof(DeletePost.Handler),
            typeof(EditComment.Handler),
            typeof(DeleteComment.Handler)
        };

        foreach (var handler in handlers)
        {
            var hasOutboxWriter = handler
                .GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType == typeof(IOutboxWriter));

            Assert.False(hasOutboxWriter);
        }
    }
}
