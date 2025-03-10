using ChatRumi.Account.Application.Projections;
using ErrorOr;
using Marten;
using MediatR;

namespace ChatRumi.Account.Application.Queries;

public class GetAccount
{
    public record Query(Guid AccountId) : IRequest<ErrorOr<AccountDto>>;

    public class Handler(
        IDocumentStore store
    ) : IRequestHandler<Query, ErrorOr<AccountDto>>
    {
        public async Task<ErrorOr<AccountDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();
            
            var account = await session.Events
                .AggregateStreamAsync<Domain.Aggregate.Account>(request.AccountId, token: cancellationToken);
            
            if (account is null)
            {
                return Error.NotFound("Account not found.",
                    description: $"Account by id '{request.AccountId}' not found.");
            }

            return new AccountDto(
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

public record AccountDto(
    Guid Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string CountryCode,
    bool IsVerified
);