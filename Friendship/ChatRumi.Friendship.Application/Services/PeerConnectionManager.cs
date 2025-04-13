using ChatRumi.Friendship.Application.Dto.Request;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace ChatRumi.Friendship.Application.Services;

public interface IPeerConnectionManager
{
    Task CreatePeerAsync(Guid peerId, string userName, DateTime createdDate);
    Task SendFriendRequestAsync(Guid peerId1, Guid peerId2);
    Task AcceptFriendRequestAsync(Guid peerId1, Guid peerId2);
    Task UnfriendAsync(Guid peerId, Guid targetPeerId);
    Task<PeerResponse[]> GetFriendsAsync(Guid peerId);
    Task<PeerResponse[]> GetFriendRequestsAsync(Guid peerId);
    Task<PeerResponse[]> GetRequestsISent(Guid peerId);
}

public class PeerConnectionManager : IPeerConnectionManager
{
    private readonly IAsyncSession _session;

    public PeerConnectionManager(
        IDriver driver,
        IOptions<Neo4jOptions> options
    )
    {
        _session = driver.AsyncSession(builder => builder.WithDatabase(options.Value.Neo4jDatabase));
    }

    public async Task CreatePeerAsync(Guid peerId, string userName, DateTime createdDate)
    {
        if (await PeerExistsAsync(peerId))
        {
            return;
        }

        const string query = "CREATE (a:Account {peerId: $peerId, userName: $userName, createdDate: $createdDate})";
        var parameters = new { peerId = peerId.ToString(), userName, createdDate };

        await _session.RunAsync(query, parameters);
    }

    public async Task UnfriendAsync(Guid peerId, Guid targetPeerId)
    {
        // Remove the FRIENDS relationship in both directions
        const string query = """
                              MATCH (a:Account {peerId: $peerId})-[r:FRIENDS_WITH]-(b:Account {peerId: $targetPeerId})
                              DELETE r
                             """;

        var parameters = new
        {
            peerId = peerId.ToString(),
            targetPeerId = targetPeerId.ToString()
        };

        await _session.RunAsync(query, parameters);
    }

    public async Task SendFriendRequestAsync(Guid peerId1, Guid peerId2)
    {
        const string query = """
                               MATCH (a1:Account {peerId: $peerId1}), (a2:Account {peerId: $peerId2})
                               MERGE (a1)-[r:FRIEND_REQUEST]->(a2)
                               ON CREATE SET r.sentAt = datetime()
                             """;

        var parameters = new
        {
            peerId1 = peerId1.ToString(),
            peerId2 = peerId2.ToString()
        };

        await _session.ExecuteWriteAsync(async tx => { await tx.RunAsync(query, parameters); });
    }

    public async Task AcceptFriendRequestAsync(Guid peerId2, Guid peerId1)
    {
        const string query = """
                                 MATCH (a1:Account {peerId: $peerId1})-[r:FRIEND_REQUEST]->(a2:Account {peerId: $peerId2})
                                 DELETE r
                                 CREATE (a1)-[:FRIENDS_WITH]->(a2)
                                 CREATE (a2)-[:FRIENDS_WITH]->(a1)
                             """;

        var parameters = new { peerId1 = peerId1.ToString(), peerId2 = peerId2.ToString() };

        await _session.RunAsync(query, parameters);
    }

    public async Task<PeerResponse[]> GetFriendsAsync(Guid peerId)
    {
        const string query = """
                                 MATCH (a:Account {peerId: $peerId})-[:FRIENDS_WITH]-(friend:Account)
                                 RETURN friend.peerId, friend.userName, friend.createdDate
                             """;

        var parameters = new { peerId = peerId.ToString() };

        var result = await _session.RunAsync(query, parameters);
        return (await result.ToListAsync(record => new PeerResponse(
                Guid.Parse(record["friend.peerId"].As<string>()),
                record["friend.userName"].As<string>(),
                record["friend.createdDate"].As<ZonedDateTime>().UtcDateTime
            )))
            .DistinctBy(a => a.PeerId)
            .ToArray();
    }

    public async Task<PeerResponse[]> GetFriendRequestsAsync(Guid peerId)
    {
        const string query = """
                                 MATCH (sender:Account)-[:FRIEND_REQUEST]->(recipient:Account {peerId: $peerId})
                                 RETURN sender.peerId AS peerId, sender.userName AS userName, sender.createdDate AS createdDate
                             """;

        var parameters = new { peerId = peerId.ToString() };

        var result = await _session.RunAsync(query, parameters);

        return (await result.ToListAsync(record => new PeerResponse(
                Guid.Parse(record["peerId"].As<string>()),
                record["userName"].As<string>(),
                record["createdDate"].As<ZonedDateTime>().UtcDateTime
            )))
            .ToArray();
    }

    public async Task<PeerResponse[]> GetRequestsISent(Guid peerId)
    {
        const string query = """
                                 MATCH (requester:Account {peerId: $peerId})-[:FRIEND_REQUEST]->(recipient:Account)
                                 RETURN recipient.peerId AS peerId, recipient.userName AS userName, recipient.createdDate AS createdDate
                             """;
        
        var parameters = new { peerId = peerId.ToString() };

        var result = await _session.RunAsync(query, parameters);

        return (await result.ToListAsync(record => new PeerResponse(
                Guid.Parse(record["peerId"].As<string>()),
                record["userName"].As<string>(),
                record["createdDate"].As<ZonedDateTime>().UtcDateTime
            )))
            .ToArray();
    }

    private async Task<bool> PeerExistsAsync(Guid peerId)
    {
        const string query = "MATCH (a:Account {peerId: $peerId}) RETURN a";
        var parameters = new { peerId = peerId.ToString() };

        var result = await _session.RunAsync(query, parameters);
        return await result.FetchAsync();
    }
}