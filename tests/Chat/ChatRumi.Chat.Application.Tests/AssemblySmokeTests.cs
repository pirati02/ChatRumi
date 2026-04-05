using Xunit;

namespace ChatRumi.Chat.Application.Tests;

public class AssemblySmokeTests
{
    [Fact]
    public void Test_project_references_application_layer()
    {
        Assert.NotNull(typeof(ChatRumi.Chat.Application.ModuleRegistration));
    }
}
