namespace ChatRumi.Chat.Application.Projections.ExistingChat;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record ExistingChatProjection
{
    public Guid Id { get; set; }
    public string ParticipantsHash { get; set; } = null!;
    public bool IsGroupChat { get; set; }
}