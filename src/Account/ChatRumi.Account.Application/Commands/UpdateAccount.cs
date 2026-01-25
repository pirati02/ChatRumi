using ChatRum.InterCommunication;
using ChatRumi.Account.Application.IntegrationEvents;
using ChatRumi.Account.Domain.Events;
using FluentValidation;
using Marten;
using MediatR;
using ErrorOr;

namespace ChatRumi.Account.Application.Commands;

public static class UpdateAccount
{
    public sealed record Command(
        Guid Id,
        string UserName,
        string FirstName,
        string LastName
    ) : IRequest<ErrorOr<Guid>>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserName).NotEmpty();
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
        }
    }

    public class Handler(
        IDocumentStore store,
        IValidator<Command> validator,
        IDispatcher dispatcher
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

            var account =
                await session.Events.AggregateStreamAsync<Domain.Aggregate.Account>(request.Id,
                    token: cancellationToken);

            if (account is null)
            {
                return Error.NotFound("Account not found.");
            }

            var @event = new AccountModifiedEvent
            {
                AccountId = account.Id,
                UserName = request.UserName,
                FirstName = request.FirstName,
                LastName = request.LastName
            };
            var action = session.Events.Append(@event.AccountId, @event);
            await session.SaveChangesAsync(cancellationToken);

            await dispatcher.ProduceAsync(
                Topics.AccountUpdatedTopic,
                account.Id.ToString(),
                new AccountModified(
                    account.Id,
                    @event.UserName,
                    @event.FirstName,
                    @event.LastName,
                    account.PublicKey
                ),
                cancellationToken
            );

            return action.Id;
        }
    }
}