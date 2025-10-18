using ChatRumi.Account.Domain.Events;
using Marten.Events.Aggregation;

namespace ChatRumi.Account.Application.Projections;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record AccountProjection
{
    public Guid Id { get; set; } // Same ID as the event stream
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string CountryCode { get; set; }
    
    public bool IsVerified { get; set; }
    public DateTimeOffset VerifiedOn { get; set; }
}

public class AccountProjectionTransform : SingleStreamProjection<AccountProjection>
{
    public AccountProjectionTransform()
    {
        ProjectEvent<AccountCreateEvent>((account, @event) =>
        {
            account.Id = @event.AccountId;
            account.UserName = @event.UserName;
            account.Email = @event.Email;
            account.FirstName = @event.FirstName;
            account.LastName = @event.LastName;
            account.PhoneNumber = @event.PhoneNumber;
            account.CountryCode = @event.CountryCode;
        });
        
        ProjectEvent<AccountModifiedEvent>((account, @event) =>
        {
            account.Id = @event.AccountId;
            account.UserName = @event.UserName;
            account.FirstName = @event.FirstName;
            account.LastName = @event.LastName;
        });
        
        ProjectEvent<VerifyAccountEvent>((account, @event) =>
        {
            account.Id = @event.AccountId;
            account.IsVerified = true;
            account.VerifiedOn = DateTimeOffset.UtcNow;
        });
    }
}