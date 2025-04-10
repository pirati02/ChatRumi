namespace ChatRumi.Account.Application.IntegrationEvents;

public record AccountCreated(Guid AccountId, string UserName);