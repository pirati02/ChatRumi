using ChatRumi.Account.Application.Projections;
using Marten;
using MassTransit;
using Mediator;

namespace ChatRumi.Account.Application.Commands;

// Inside the namespace so it wins over MassTransit.Response<T>.
using ChatRumi.Kernel.Results;

public static class SendVerificationRequest
{
    public sealed record Command(Guid AccountId) : IRequest<Response<bool>>;

    public sealed class Handler(
        IDocumentStore store,
        IPublishEndpoint publisher
    ) : IRequestHandler<Command, Response<bool>>
    {
        public async ValueTask<Response<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();
            var account = await session.Query<AccountProjection>().FirstOrDefaultAsync(
                a => a.Id == request.AccountId,
                token: cancellationToken
            );

            if (account is null)
            {
                return ResponseError.NotFound("Account not found.", $"Account with id {request.AccountId} not found.");
            }

            if (account.IsVerified)
            {
                return ResponseError.Conflict("Account is already verified.",
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