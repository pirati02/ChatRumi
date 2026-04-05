using Xunit;

namespace ChatRumi.Friendship.Application.Tests;

public class AssemblySmokeTests
{
    [Fact]
    public void Test_project_references_application_layer()
    {
        Assert.NotNull(typeof(ChatRumi.Friendship.Application.ModuleRegistration));
    }
}
