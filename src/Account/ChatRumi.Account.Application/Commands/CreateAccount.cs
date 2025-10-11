using ChatRumi.Account.Application.Projections;
using ChatRumi.Account.Application.Services;
using ChatRumi.Account.Domain.Events;
using ErrorOr;
using FluentValidation;
using Marten;
using MassTransit;
using MediatR;

namespace ChatRumi.Account.Application.Commands;

public static class CreateAccount
{
    public sealed record Command(
        string UserName,
        string Email,
        string FirstName,
        string LastName,
        string CountryCode,
        string PhoneNumber,
        string Password
    ) : IRequest<ErrorOr<Guid>>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserName).NotEmpty();
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.PhoneNumber)
                .NotEmpty();
            RuleFor(x => x.CountryCode)
                .NotEmpty();
            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(4)
                .MaximumLength(20)
                .Matches(@"^[A-Z][a-z]+\d+.*[\W_]+.*$");
        }
    }

    public class Handler(
        IDocumentStore store,
        IValidator<Command> validator,
        IPublishEndpoint publisher
    ) : IRequestHandler<Command, ErrorOr<Guid>>
    {
        public async Task<ErrorOr<Guid>> Handle(Command request, CancellationToken cancellationToken)
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

            var exists = await session.Query<AccountProjection>().AnyAsync(
                a =>
                    a.UserName == request.UserName ||
                    a.Email == request.Email,
                token: cancellationToken
            );

            if (exists)
            {
                return Error.Conflict("Account already exists.");
            }

            PasswordHasher.CreatePasswordHash(request.Password, out var passwordHash, out var passwordSalt);
            var @event = new AccountCreateEvent
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CountryCode = request.CountryCode,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt
            };
            var action = session.Events.StartStream<Domain.Aggregate.Account>(@event.AccountId, @event);
            await session.SaveChangesAsync(cancellationToken);

            await publisher.Publish(new Events.VerifyAccount.Event
            {
                AccountId = @event.AccountId,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                CountryCode = request.CountryCode
            }, cancellationToken);

            return action.Id;
        }
    }
}