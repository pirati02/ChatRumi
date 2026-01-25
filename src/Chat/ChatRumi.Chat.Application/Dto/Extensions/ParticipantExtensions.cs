using ChatRumi.Chat.Domain.Aggregates;

namespace ChatRumi.Chat.Application.Dto.Extensions;

public static class ParticipantExtensions
{
    extension(ParticipantDto participant)
    {
        public Participant ToDomain()
        {
            return new Participant
            {
                Id = participant.Id,
                FirstName = participant.FirstName,
                LastName = participant.LastName,
                NickName = participant.NickName,
                PublicKey = participant.PublicKey
            };
        }
    }

    extension(Participant participant)
    {
        public ParticipantDto ToDto()
        {
            return new ParticipantDto(
                participant.Id,
                participant.FirstName,
                participant.LastName,
                participant.NickName,
                participant.PublicKey
            );
        }
    }
}