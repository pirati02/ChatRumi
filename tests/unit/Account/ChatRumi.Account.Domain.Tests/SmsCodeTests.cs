using ChatRumi.Account.Domain.ValueObjects;
using Xunit;

namespace ChatRumi.Account.Domain.Tests;

public class SmsCodeTests
{
    [Fact]
    public void Key_ReturnsPhoneScopedIdentifier()
    {
        var code = new SmsCode("+15551234567", "123456");
        Assert.Equal("+15551234567-smscode", code.Key());
    }
}
