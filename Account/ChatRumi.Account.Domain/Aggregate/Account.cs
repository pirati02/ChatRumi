using ChatRumi.Account.Domain.Events;
using Marten.Events;

namespace ChatRumi.Account.Domain.Aggregate;

public class Account
{
    public Account()
    {
        
    }

    public static Account Create(IEvent<AccountCreateEvent> created)
    {
        var @event = created.Data;
        return new Account
        {
            Email = @event.Email,
            UserName = @event.UserName,
            CountryCode = @event.CountryCode,
            FirstName = @event.FirstName,
            LastName = @event.LastName,
            PhoneNumber = @event.PhoneNumber,
            PasswordHash = @event.PasswordHash,
            PasswordSalt = @event.PasswordSalt
        };
    }

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string CountryCode { get; set; }
    
    public bool IsVerified { get; set; } = false;
    public DateTimeOffset VerifiedOn { get; set; }
    public bool MfaEnabled { get; set; } = false;
    
    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];

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
}