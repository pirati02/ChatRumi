namespace ChatRumi.Account.Application.IntegrationEvents;

public record AccountModified(
    Guid AccountId,
    string UserName,
    string FirstName,
    string LastName
);
