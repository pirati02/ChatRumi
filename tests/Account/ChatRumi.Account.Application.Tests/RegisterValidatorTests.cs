using ChatRumi.Account.Application.Commands;
using Xunit;

namespace ChatRumi.Account.Application.Tests;

public class RegisterValidatorTests
{
    private static Register.Command ValidCommand() =>
        new(
            UserName: "alice",
            Email: "alice@example.com",
            FirstName: "Alice",
            LastName: "Smith",
            CountryCode: "US",
            PhoneNumber: "+15551234567",
            Password: "Abcdef1!"
        );

    [Fact]
    public async Task ValidateAsync_ValidCommand_Succeeds()
    {
        var validator = new Register.Validator();
        var result = await validator.ValidateAsync(ValidCommand());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task ValidateAsync_InvalidEmail_Fails(string email)
    {
        var validator = new Register.Validator();
        var c = ValidCommand() with { Email = email };
        var result = await validator.ValidateAsync(c);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Register.Command.Email));
    }

    [Fact]
    public async Task ValidateAsync_PasswordNotMatchingPattern_Fails()
    {
        var validator = new Register.Validator();
        var c = ValidCommand() with { Password = "short" };
        var result = await validator.ValidateAsync(c);
        Assert.False(result.IsValid);
    }
}
