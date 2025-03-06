namespace ChatRumi.Account.Domain.Events;

public class VerifyAccountEvent
{
    public Guid AccountId { get; set; }
    public long Version { get; set; } = 1;
}