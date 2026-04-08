using ChatRum.InterCommunication;
using ChatRumi.Account.Application.IntegrationEvents;
using ChatRumi.Account.Domain.Events;
using FluentValidation;
using Marten;
using ErrorOr;
using Mediator;

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
        IDocumentSession session,
        IValidator<Command> validator,
        IOutboxWriter outboxWriter
    ) : IRequestHandler<Command, ErrorOr<Guid>>
    {
        public async ValueTask<ErrorOr<Guid>> Handle(Command request, CancellationToken cancellationToken)
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

            await outboxWriter.EnqueueAsync(
                Topics.AccountUpdatedTopic,
                account.Id.ToString(),
                new AccountModified(
                    account.Id,
                    @event.UserName,
                    @event.FirstName,
                    @event.LastName
                ),
                cancellationToken
            );
            await session.SaveChangesAsync(cancellationToken);

            return action.Id;
        }
    }
}