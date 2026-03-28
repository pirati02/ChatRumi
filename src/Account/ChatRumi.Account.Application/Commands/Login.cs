using ChatRumi.Account.Application.Projections;
using ChatRumi.Account.Application.Services;
using ErrorOr;
using FluentValidation;
using Marten;
using Mediator;

namespace ChatRumi.Account.Application.Commands;

public static class Login
{
    public sealed record Command(string Email, string Password) : IRequest<ErrorOr<LoginResponse>>;

    public sealed record LoginResponse(string access_token, string token_type, int expires_in);

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
        IJwtAccessTokenIssuer jwtAccessTokenIssuer
    ) : IRequestHandler<Command, ErrorOr<LoginResponse>>
    {
        public async ValueTask<ErrorOr<LoginResponse>> Handle(Command request, CancellationToken cancellationToken)
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

            return new LoginResponse(accessToken, "Bearer", expiresIn);
        }
    }
}
