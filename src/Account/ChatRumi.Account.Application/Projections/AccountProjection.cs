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

public class AccountProjectionTransform : SingleStreamProjection<AccountProjection, Guid>
{
    public AccountProjection Create(AccountCreateEvent @event) => new()
    {
        Id = @event.AccountId,
        UserName = @event.UserName,
        Email = @event.Email,
        FirstName = @event.FirstName,
        LastName = @event.LastName,
        PhoneNumber = @event.PhoneNumber,
        CountryCode = @event.CountryCode
    };

    public void Apply(AccountModifiedEvent @event, AccountProjection account)
    {
        account.Id = @event.AccountId;
        account.UserName = @event.UserName;
        account.FirstName = @event.FirstName;
        account.LastName = @event.LastName;
    }

    public void Apply(VerifyAccountEvent @event, AccountProjection account)
    {
        account.Id = @event.AccountId;
        account.IsVerified = true;
        account.VerifiedOn = DateTimeOffset.UtcNow;
    }
}