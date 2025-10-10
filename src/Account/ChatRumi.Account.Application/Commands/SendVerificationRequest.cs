using ChatRumi.Account.Application.Projections;
using ErrorOr;
using Marten;
using MassTransit;
using MediatR;

namespace ChatRumi.Account.Application.Commands;

public sealed class SendVerificationRequest
{
    public sealed record Command(Guid AccountId) : IRequest<ErrorOr<bool>>;

    public sealed class Handler(
        IDocumentStore store,
        IPublishEndpoint publisher
    ) : IRequestHandler<Command, ErrorOr<bool>>
    {
        public async Task<ErrorOr<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();
            var account = await session.Query<AccountProjection>().FirstOrDefaultAsync(
                a => a.Id == request.AccountId,
                token: cancellationToken
            );

            if (account is null)
            {
                return Error.NotFound("Account not found.", $"Account with id {request.AccountId} not found.");
            }

            if (account.IsVerified)
            {
                return Error.Conflict("Account is already verified.",
                    $"Account with id {request.AccountId} is aleady verified.");
            }

            await publisher.Publish(new Events.VerifyAccount.Event
            {
                AccountId = account.Id,
                Email = account.Email,
                PhoneNumber = account.PhoneNumber,
                CountryCode = account.CountryCode,
            }, cancellationToken);

            return true;
        }
    }
}