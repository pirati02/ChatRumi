using ChatRumi.Feed.Application.Commands;
using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Feed.Domain.ValueObject;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nest;
using Xunit;

namespace ChatRumi.Feed.Application.Tests;

public class CreatePostHandlerTests
{
    [Fact]
    public async Task Handle_WhenElasticsearchReturnsValid_ReturnsPostIdString()
    {
        var creator = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "A",
            LastName = "B"
        };

        var createResponse = new Mock<CreateResponse>();
        createResponse.SetupGet(r => r.IsValid).Returns(true);

        var client = new Mock<IElasticClient>();
        client
            .Setup(c => c.CreateDocumentAsync(It.IsAny<PostDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createResponse.Object);

        var handler = new CreatePost.Handler(client.Object, NullLogger<CreatePost.Handler>.Instance);
        var result = await handler.Handle(new CreatePost.Command(creator, "Hello", [Guid.NewGuid()]), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
        Assert.True(Guid.TryParse(result.Value, out var parsed));
        Assert.NotEqual(Guid.Empty, parsed);
    }

    [Fact]
    public async Task Handle_WhenElasticsearchReturnsInvalid_ReturnsUnexpectedError()
    {
        var creator = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "A",
            LastName = "B"
        };

        var createResponse = new Mock<CreateResponse>();
        createResponse.SetupGet(r => r.IsValid).Returns(false);

        var client = new Mock<IElasticClient>();
        client
            .Setup(c => c.CreateDocumentAsync(It.IsAny<PostDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createResponse.Object);

        var handler = new CreatePost.Handler(client.Object, NullLogger<CreatePost.Handler>.Instance);
        var result = await handler.Handle(new CreatePost.Command(creator, "D", null), CancellationToken.None);

        Assert.True(result.IsError);
    }
}
