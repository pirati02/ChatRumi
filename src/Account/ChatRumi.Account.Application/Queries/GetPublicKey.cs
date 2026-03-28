using ErrorOr;
using Marten;
using Mediator;

namespace ChatRumi.Account.Application.Queries;

/// <summary>
/// Returns the registered E2E public key (Base64 SPKI) for an account.
/// </summary>
public static class GetPublicKey
{
    public record Query(Guid AccountId) : IRequest<ErrorOr<PublicKeyResponse>>;

    public sealed class Handler(IDocumentStore store) : IRequestHandler<Query, ErrorOr<PublicKeyResponse>>
    {
        public async ValueTask<ErrorOr<PublicKeyResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            var account = await session.Events
                .AggregateStreamAsync<Domain.Aggregate.Account>(request.AccountId, token: cancellationToken);

            if (account is null)
            {
                return Error.NotFound("Account not found.",
                    description: $"Account by id '{request.AccountId}' not found.");
            }

            return new PublicKeyResponse(account.PublicKey);
        }
    }
}

/// <param name="PublicKey">Base64 SPKI when registered; null when account exists but no key was stored.</param>
public record PublicKeyResponse(string? PublicKey);
