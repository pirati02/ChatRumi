using ChatRumi.Account.Application.Projections;
using ErrorOr;
using Marten;
using MediatR;

namespace ChatRumi.Account.Application.Queries;

public class GetAccount
{
    public record Query(Guid AccountId) : IRequest<ErrorOr<AccountProjection>>;

    public class Handler(
        IDocumentStore store
    ) : IRequestHandler<Query, ErrorOr<AccountProjection>>
    {
        public async Task<ErrorOr<AccountProjection>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();
            var account = await session.Query<AccountProjection>().FirstOrDefaultAsync(a => a.Id == request.AccountId, token: cancellationToken);
            if (account is null)
            {
                return Error.NotFound("Account not found.", description: $"Account by id '{request.AccountId}' not found.");
            }

            return account;
        }
    }
}