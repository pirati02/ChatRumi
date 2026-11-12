using ChatRumi.Kernel.Results;
using Marten;
using Mediator;

namespace ChatRumi.Account.Application.Queries;

public static class GetAccount
{
    public record Query(Guid AccountId) : IRequest<Response<AccountResponse>>;

    public class Handler(
        IDocumentStore store
    ) : IRequestHandler<Query, Response<AccountResponse>>
    {
        public async ValueTask<Response<AccountResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            var account = await session.Events
                .AggregateStreamAsync<Domain.Aggregate.Account>(request.AccountId, token: cancellationToken);

            if (account is null)
            {
                return ResponseError.NotFound("Account not found.",
                    description: $"Account by id '{request.AccountId}' not found.");
            }

            return new AccountResponse(
                account.Id,
                account.UserName,
                account.Email,
                account.FirstName,
                account.LastName,
                account.PhoneNumber,
                account.CountryCode,
                account.IsVerified
            );
        }
    }
}

public record AccountResponse(
    Guid Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string CountryCode,
    bool IsVerified
);