namespace ChatRumi.Account.Domain.Events;

public class AccountCreateEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string CountryCode { get; set; }
    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];
    
    public long Version { get; set; } = 1;
}