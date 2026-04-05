using ChatRumi.Kernel;

namespace ChatRumi.Account.Domain.Events;

public record PasswordRehashedEvent : DomainEvent
{
    public required byte[] PasswordHash { get; init; }
    public required byte[] PasswordSalt { get; init; }
}
