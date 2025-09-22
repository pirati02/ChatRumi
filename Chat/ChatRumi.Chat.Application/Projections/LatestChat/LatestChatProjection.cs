using ChatRumi.Chat.Domain.Aggregates;

namespace ChatRumi.Chat.Application.Projections.LatestChat;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record LatestChatProjection
{
    public Guid Id { get; set; }
    public List<Participant> Participants { get; set; } = null!;
    public LatestMessage? LatestMessage { get; set; }
}