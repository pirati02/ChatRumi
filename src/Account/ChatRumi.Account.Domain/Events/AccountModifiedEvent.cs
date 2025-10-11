using ChatRumi.Kernel;

namespace ChatRumi.Account.Domain.Events;

public record AccountModifiedEvent : DomainEvent
{
    public Guid AccountId { get; set; }
    public required string UserName { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}