namespace ChatRumi.Chat.Application.Dto.Request;

// ReSharper disable once ClassNeverInstantiated.Global
public record ParticipantDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? NickName
);