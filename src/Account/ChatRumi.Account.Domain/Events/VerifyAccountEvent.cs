using ChatRumi.Kernel;

namespace ChatRumi.Account.Domain.Events;

public record VerifyAccountEvent: DomainEvent
{
    public Guid AccountId { get; init; }
}