using ChatRumi.Kernel;

namespace ChatRumi.Account.Domain.Events;

public record AccountKeyRegisteredEvent : DomainEvent
{
    public required Guid AccountId { get; init; }
    public required string PublicKey { get; init; }
}
