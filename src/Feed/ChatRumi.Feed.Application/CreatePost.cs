using MediatR;

namespace ChatRumi.Feed.Application;

public static class CreatePost
{
    public sealed record Command(
        string Title,
        string Description
    ) : IRequest<string>;

    public sealed class Handler : IRequestHandler<Command, string>
    {
        public Task<string> Handle(Command request, CancellationToken cancellationToken)
        {
            return Task.FromResult("");
        }
    }
}