using ChatRumi.Feed.Domain.ValueObject;
using MediatR;

namespace ChatRumi.Feed.Application;

public static class CreatePost
{
    public sealed record Command(
        Participant Creator,
        string Title,
        string Description,
        IEnumerable<Attachment> Attachments
    ) : IRequest<string>;

    public sealed class Handler : IRequestHandler<Command, string>
    {
        public Task<string> Handle(Command request, CancellationToken cancellationToken)
        {
            return Task.FromResult("");
        }
    }
}