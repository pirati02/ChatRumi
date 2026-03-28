using ChatRumi.Account.Application.Documents;
using ChatRumi.Account.Application.Projections;
using ChatRumi.Account.Application.Services;
using ChatRumi.Infrastructure;
using ErrorOr;
using FluentValidation;
using Marten;
using Mediator;
using Microsoft.Extensions.Options;

namespace ChatRumi.Account.Application.Commands;

public static class Refresh
{
    public sealed record Command(string refresh_token) : IRequest<ErrorOr<AuthTokenResponse>>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.refresh_token).NotEmpty();
        }
    }

    public sealed class Handler(
        IDocumentStore store,
        IValidator<Command> validator,
        IJwtAccessTokenIssuer jwtAccessTokenIssuer,
        IOptions<JwtOptions> jwtOptions
    ) : IRequestHandler<Command, ErrorOr<AuthTokenResponse>>
    {
        public async ValueTask<ErrorOr<AuthTokenResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return validationResult.Errors
                    .ConvertAll(error =>
                        Error.Validation(
                            code: error.PropertyName,
                            description: error.ErrorMessage
                        )
                    );
            }

            var hash = RefreshTokenCrypto.HashToken(request.refresh_token);
            await using var session = store.LightweightSession();

            var stored = await session.Query<StoredRefreshToken>()
                .Where(x => x.TokenHash == hash)
                .FirstOrDefaultAsync(cancellationToken);

            if (stored is null || stored.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return Error.Unauthorized(
                    code: "Refresh.InvalidToken",
                    description: "Invalid or expired refresh token.");
            }

            var projection = await session.Query<AccountProjection>()
                .FirstOrDefaultAsync(a => a.Id == stored.AccountId, cancellationToken);

            Domain.Aggregate.Account? account = null;
            if (projection is not null)
            {
                account = await session.Events.AggregateStreamAsync<Domain.Aggregate.Account>(
                    projection.Id,
                    token: cancellationToken);
            }

            if (projection is null || account is null)
            {
                return Error.Unauthorized(
                    code: "Refresh.InvalidToken",
                    description: "Invalid or expired refresh token.");
            }

            session.Delete(stored);

            var accessToken = jwtAccessTokenIssuer.CreateAccessToken(
                account.Id,
                account.Email,
                account.UserName,
                out var expiresIn);

            var plainRefresh = RefreshTokenCrypto.CreateRefreshToken();
            var refreshHash = RefreshTokenCrypto.HashToken(plainRefresh);
            var jwt = jwtOptions.Value;
            var refreshExpiresSeconds = jwt.RefreshTokenExpirationDays * 24 * 3600;
            var now = DateTimeOffset.UtcNow;

            session.Store(new StoredRefreshToken
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                TokenHash = refreshHash,
                ExpiresAt = now.AddDays(jwt.RefreshTokenExpirationDays),
                CreatedAt = now
            });

            await session.SaveChangesAsync(cancellationToken);

            return new AuthTokenResponse(accessToken, "Bearer", expiresIn, plainRefresh, refreshExpiresSeconds);
        }
    }
}
