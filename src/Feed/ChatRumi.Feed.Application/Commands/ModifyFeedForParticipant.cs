using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Feed.Domain.ValueObject;
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
            var searchResponse = await client.SearchAsync<PostDocument>(s => s
                    .Index(PostIndexes.Posts)
                    .Query(q => q.Match(t => t.Field(f => f.Creator.Id).Query(request.ParticipantId.ToString())))
                    .Size(1000),
                cancellationToken);

            if (!searchResponse.IsValid || searchResponse.Documents.Count == 0)
            {
                logger.LogInformation("No posts found for participant {Id}", request.ParticipantId);
                return;
            }
            var documents = searchResponse.Documents.ToList();
            logger.LogInformation("Found {Count} posts for participant {Id}", documents.Count, request.ParticipantId);
 
            var bulkDescriptor = new BulkDescriptor();
            foreach (var doc in documents)
            {
                doc.Creator = new Participant { Id = request.ParticipantId, FirstName = request.FirstName, LastName = request.LastName, NickName = request.UserName };

                bulkDescriptor.Update<PostDocument>(u => u
                    .Index(PostIndexes.Posts)
                    .Id(doc.Id)
                    .Doc(doc)
                );
            }
 
            var bulkResponse = await client.BulkAsync(bulkDescriptor, cancellationToken);

            if (bulkResponse.Errors)
            {
                foreach (var item in bulkResponse.ItemsWithErrors)
                {
                    logger.LogError("Failed to update document {Id}: {Error}", item.Id, item.Error.Reason);
                }
            }
            else
            {
                logger.LogInformation("Successfully updated {Count} posts for participant {Id}",
                    documents.Count, request.ParticipantId);
            }
        }
    }
}