using System.Runtime.CompilerServices;
using ChatRumi.Account.Domain.Events;
using Xunit;
using AccountEntity = ChatRumi.Account.Domain.Aggregate.Account;

namespace ChatRumi.Account.Domain.Tests;

public class AccountAggregateTests
{
    [Fact]
    public void Apply_AccountCreateEvent_SetsStateAndClearsVerification()
    {
        var account = UnsafeAccountEntity();
        var accountId = Guid.NewGuid();
        var hash = new byte[] { 1, 2, 3 };
        var salt = new byte[] { 4, 5, 6 };

        account.Apply(
            new AccountCreateEvent
            {
                AccountId = accountId,
                UserName = "alice",
                Email = "a@example.com",
                FirstName = "Alice",
                LastName = "Smith",
                PhoneNumber = "+1000",
                CountryCode = "US",
                PasswordHash = hash,
                PasswordSalt = salt
            });

        Assert.Equal(accountId, account.Id);
        Assert.Equal("alice", account.UserName);
        Assert.Equal("a@example.com", account.Email);
        Assert.Equal("Alice", account.FirstName);
        Assert.Equal("Smith", account.LastName);
        Assert.Equal("+1000", account.PhoneNumber);
        Assert.Equal("US", account.CountryCode);
        Assert.Same(hash, account.PasswordHash);
        Assert.Same(salt, account.PasswordSalt);
        Assert.False(account.IsVerified);
        Assert.False(account.MfaEnabled);
    }

    [Fact]
    public void Apply_VerifyAccountEvent_SetsVerified()
    {
        var account = UnsafeAccountEntity();
        account.Apply(
            new AccountCreateEvent
            {
                AccountId = Guid.NewGuid(),
                UserName = "u",
                Email = "u@e.com",
                FirstName = "U",
                LastName = "V",
                PhoneNumber = "+1",
                CountryCode = "US"
            });

        account.Apply(new VerifyAccountEvent { AccountId = account.Id });

        Assert.True(account.IsVerified);
        Assert.True(account.VerifiedOn <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Apply_AccountModifiedEvent_UpdatesProfileFields()
    {
        var account = UnsafeAccountEntity();
        account.Apply(
            new AccountCreateEvent
            {
                AccountId = Guid.NewGuid(),
                UserName = "old",
                Email = "e@e.com",
                FirstName = "O",
                LastName = "L",
                PhoneNumber = "+1",
                CountryCode = "US"
            });

        account.Apply(
            new AccountModifiedEvent
            {
                AccountId = account.Id,
                UserName = "new",
                FirstName = "N",
                LastName = "W"
            });

        Assert.Equal("new", account.UserName);
        Assert.Equal("N", account.FirstName);
        Assert.Equal("W", account.LastName);
    }

    [Fact]
    public void Apply_AccountKeyRegisteredEvent_DoesNotThrow()
    {
        var account = UnsafeAccountEntity();
        account.Apply(
            new AccountCreateEvent
            {
                AccountId = Guid.NewGuid(),
                UserName = "u",
                Email = "u@e.com",
                FirstName = "U",
                LastName = "V",
                PhoneNumber = "+1",
                CountryCode = "US"
            });

        var ex = Record.Exception(() =>
            account.Apply(
                new AccountKeyRegisteredEvent
                {
                    AccountId = account.Id,
                    PublicKey = "pk"
                }));

        Assert.Null(ex);
    }

    private static AccountEntity UnsafeAccountEntity()
    {
        return (AccountEntity)RuntimeHelpers.GetUninitializedObject(typeof(AccountEntity));
    }
}
