using ChatRum.InterCommunication;
using ChatRumi.Account.Application.IntegrationEvents;
using ChatRumi.Account.Application.Projections;
using ChatRumi.Account.Domain.Events;
using ChatRumi.Account.Domain.ValueObjects;
using ErrorOr;
using Marten;
using MediatR;
using StackExchange.Redis;

namespace ChatRumi.Account.Application.Commands;

public static class VerifyAccount
{
    public sealed record Command(string Code, Guid AccountId) : IRequest<ErrorOr<bool>>;

    public sealed class Handler(
        IDocumentStore store,
        IConnectionMultiplexer connectionMultiplexer,
        IDispatcher dispatcher
    ) : IRequestHandler<Command, ErrorOr<bool>>
    {
        public async Task<ErrorOr<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();
            var account = await session.Query<AccountProjection>()
                .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

            if (account is null)
            {
                return Error.NotFound("Account not found.", $"Account with id {request.AccountId} not found.");
            }

            if (account.IsVerified)
            {
                return Error.Conflict("Account is already verified.", $"Account with id {request.AccountId} is aleady verified.");
            }

            await using (connectionMultiplexer)
            {
                var database = connectionMultiplexer.GetDatabase(0);

                var smsCode = new SmsCode(account.PhoneNumber, request.Code);
                var accountCode = await database.StringGetDeleteAsync(smsCode.Key());
                if (!accountCode.HasValue || accountCode != request.Code)
                {
                    return Error.Conflict("Invalid verification code.", "Invalid verification code.");
                }

                var @event = new VerifyAccountEvent
                {
                    AccountId = account.Id
                };
                session.Events.Append(account.Id, @event);
                await session.SaveChangesAsync(cancellationToken);

                await dispatcher.ProduceAsync(Topics.AccountCreatedTopic, account.Id.ToString(), new AccountCreated(account.Id, account.UserName), cancellationToken);
                return true;
            }
        }
    }
}