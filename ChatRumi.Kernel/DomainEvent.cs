namespace ChatRumi.Kernel;

public record DomainEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public long Version { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}