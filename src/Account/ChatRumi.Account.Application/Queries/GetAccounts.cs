using ChatRumi.Account.Application.Projections;
using Marten;
using Mediator; 

namespace ChatRumi.Account.Application.Queries;

public static class GetAccounts
{
    public record Query : IRequest<IEnumerable<AccountResponse>>;

    public class Handler(
        IDocumentStore store
    ) : IRequestHandler<Query, IEnumerable<AccountResponse>>
    {
        public async ValueTask<IEnumerable<AccountResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            return session.Query<AccountProjection>()
                .Select(account => new AccountResponse(
                    account.Id,
                    account.UserName,
                    account.Email,
                    account.FirstName,
                    account.LastName,
                    account.PhoneNumber,
                    account.CountryCode,
                    account.IsVerified,
                    account.PublicKey
                ))
                .ToArray();
        }
    }
}