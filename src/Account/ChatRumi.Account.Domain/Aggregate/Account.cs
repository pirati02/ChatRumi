using ChatRumi.Account.Domain.Events;

namespace ChatRumi.Account.Domain.Aggregate;

// ReSharper disable once ClassNeverInstantiated.Global
public class Account : Kernel.Aggregate
{
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string CountryCode { get; set; }

    public bool IsVerified { get; private set; } = false;
    public DateTimeOffset VerifiedOn { get; set; }
    public bool MfaEnabled { get; set; } = false;

    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];

    /// <summary>
    /// Public key for end-to-end encryption (Base64 encoded)
    /// </summary>
    public string? PublicKey { get; set; }

    public void Apply(AccountCreateEvent @event)
    {
        UserName = @event.UserName;
        Email = @event.Email;
        FirstName = @event.FirstName;
        LastName = @event.LastName;
        PhoneNumber = @event.PhoneNumber;
        CountryCode = @event.CountryCode;

        IsVerified = false;
        MfaEnabled = false;
    }

    public void Apply(VerifyAccountEvent @event)
    {
        IsVerified = true;
        VerifiedOn = DateTimeOffset.UtcNow;
    }

    public void Apply(AccountModifiedEvent @event)
    {
        UserName = @event.UserName;
        FirstName = @event.FirstName;
        LastName = @event.LastName;
    }

    public void Apply(AccountKeyRegisteredEvent @event)
    {
        PublicKey = @event.PublicKey;
    }
}