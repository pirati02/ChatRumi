namespace ChatRumi.Friendship.Application.IntegrationEvents;

public record AccountCreated(Guid AccountId, string UserName);