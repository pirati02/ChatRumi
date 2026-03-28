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

public static class Login
{
    public sealed record Command(string Email, string Password) : IRequest<ErrorOr<AuthTokenResponse>>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
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

            await using var session = store.LightweightSession();
            var emailLower = request.Email.Trim().ToLowerInvariant();

            var projection = await session.Query<AccountProjection>()
                .FirstOrDefaultAsync(
                    a => a.Email.ToLower() == emailLower,
                    cancellationToken);

            Domain.Aggregate.Account? account = null;
            if (projection is not null)
            {
                account = await session.Events.AggregateStreamAsync<Domain.Aggregate.Account>(
                    projection.Id,
                    token: cancellationToken);
            }

            if (projection is null
                || account is null
                || !PasswordHasher.VerifyPassword(request.Password, account.PasswordHash, account.PasswordSalt))
            {
                return Error.Unauthorized(
                    code: "Login.InvalidCredentials",
                    description: "Invalid email or password.");
            }

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
