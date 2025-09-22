using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Domain.Aggregates;

namespace ChatRumi.Chat.Application.Dto.Extensions;

public static class ParticipantExtensions
{
    public static Participant ToDomain(this ParticipantDto participant)
    {
        return new Participant
        {
            Id = participant.Id,
            FirstName = participant.FirstName,
            LastName = participant.LastName,
            NickName = participant.NickName
        };
    }

    public static ParticipantDto ToDto(this Participant participant)
    {
        return new ParticipantDto(
            participant.Id,
            participant.FirstName,
            participant.LastName,
            participant.NickName
        );
    }
}