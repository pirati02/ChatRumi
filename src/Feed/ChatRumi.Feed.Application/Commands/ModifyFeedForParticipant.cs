using ChatRumi.Feed.Application.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using Nest;
using IRequest = MediatR.IRequest;

namespace ChatRumi.Feed.Application.Commands;

public static class ModifyFeedForParticipant
{
    public sealed record Command(
        Guid ParticipantId,
        string UserName,
        string FirstName,
        string LastName
    ) : IRequest;

    public class Handler(
        IElasticClient client,
        ILogger<Handler> logger
    ) : IRequestHandler<Command>
    {
        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var response = await client.SearchAsync<PostDocument>(s => s
                .Index("posts")
                .Query(q => q
                    .Term(t => t.Field(f => f.Creator.Id).Value(request.ParticipantId.ToString()))
                ), cancellationToken);

            var documents = response.Documents;

            foreach (var document in documents)
            {
                document.Creator = document.Creator with
                {
                    Id = request.ParticipantId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    NickName = request.UserName
                };
                var response1 = await client.UpdateAsync<PostDocument>(
                    id: document.Id,
                    selector: u => u
                        .Index("posts")
                        .Doc(document),
                    ct: cancellationToken
                );

                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}