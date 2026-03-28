using ChatRum.InterCommunication;
using ChatRumi.Account.Application.IntegrationEvents;
using ChatRumi.Account.Domain.Events;
using ErrorOr;
using Marten;
using Mediator; 

namespace ChatRumi.Account.Application.Commands;

public static class RegisterPublicKey
{
    public sealed record Command(Guid AccountId, string PublicKey) : IRequest<ErrorOr<bool>>;

    public sealed class Handler(
        IDocumentStore store,
        IDispatcher dispatcher
    ) : IRequestHandler<Command, ErrorOr<bool>>
    {
        public async ValueTask<ErrorOr<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            var account = await session.Events
                .AggregateStreamAsync<Domain.Aggregate.Account>(request.AccountId, token: cancellationToken);

            if (account is null)
            {
                return Error.NotFound("Account not found.",
                    description: $"Account by id '{request.AccountId}' not found.");
            }

            var @event = new AccountKeyRegisteredEvent
            {
                AccountId = request.AccountId,
                PublicKey = request.PublicKey
            };

            session.Events.Append(request.AccountId, @event);
            await session.SaveChangesAsync(cancellationToken);

            // Propagate public key to other services
            await dispatcher.ProduceAsync(
                Topics.AccountUpdatedTopic,
                account.Id.ToString(),
                new AccountModified(
                    account.Id,
                    account.UserName,
                    account.FirstName,
                    account.LastName,
                    request.PublicKey
                ),
                cancellationToken
            );

            return true;
        }
    }
}

public record RegisterPublicKeyRequest(string PublicKey);