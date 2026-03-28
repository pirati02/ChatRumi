namespace ChatRumi.Friendship.Application.IntegrationEvents;

public sealed record AccountModified(
    Guid AccountId,
    string UserName,
    string FirstName,
    string LastName
);
