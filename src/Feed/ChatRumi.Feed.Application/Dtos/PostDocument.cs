using ChatRumi.Feed.Domain.ValueObject;

namespace ChatRumi.Feed.Application.Dtos;

public sealed class PostDocument
{
    public Guid Id { get; init; }
    public Participant Creator { get; set; } = null!;
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;

    public List<Reaction> Reactions { get; set; } = [];
    public List<Share> Shares { get; set; } = [];

    public required string Title { get; set; }
    public required string Description { get; set; }

    public List<AttachmentId> Attachments { get; set; } = [];

    public PostDocument ModifyCreator(
        Guid participantId,
        string firstName,
        string lastName,
        string userName
    )
    {
        Creator = new Participant
        {
            Id = participantId,
            FirstName = firstName,
            LastName = lastName,
            NickName = userName
        };

        return this;
    }
}