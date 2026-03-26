namespace ChatRumi.Kernel;

public class Aggregate
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    private readonly List<DomainEvent> _events = [];
    public IReadOnlyList<DomainEvent> Events => _events.AsReadOnly();

    public void Fire<TEvent>(TEvent @event) where TEvent : DomainEvent
    {
        _events.Add(@event);
    }
}