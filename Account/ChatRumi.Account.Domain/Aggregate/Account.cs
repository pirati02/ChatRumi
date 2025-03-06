using ChatRumi.Account.Domain.Events;

namespace ChatRumi.Account.Domain.Aggregate;

public class Account
{
    public Account()
    {
        
    }
    
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string CountryCode { get; set; }
    
    public bool IsVerified { get; set; } = false;
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
}