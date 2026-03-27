using ChatRumi.Friendship.Application.Dto.Request;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace ChatRumi.Friendship.Application.Services;

public interface IPeerConnectionManager
{
    Task CreatePeerAsync(PeerDto peer);
    Task UpdatePeerAsync(PeerDto peer);
    Task SendFriendRequestAsync(PeerDto peer1, PeerDto peer2);
    Task AcceptFriendRequestAsync(PeerDto peer1, PeerDto peer2);
    Task UnfriendAsync(PeerDto peer, PeerDto targetPeer);
    Task<PeerResponse[]> GetFriendsAsync(Guid peerId);
    Task<PeerResponse[]> GetFriendRequestsAsync(Guid peerId);
    Task<PeerResponse[]> GetRequestsISent(Guid peerId);
}

public class PeerConnectionManager(
    IDriver driver,
    IOptions<Neo4jOptions> options,
    IFriendshipHubContextProxy hubContext
) : IPeerConnectionManager
{
    private readonly IAsyncSession _session = driver.AsyncSession(builder => builder.WithDatabase(options.Value.Neo4jDatabase));

    public async Task CreatePeerAsync(PeerDto peer)
    {
        if (await PeerExistsAsync(peer.PeerId))
        {
            return;
        }

        const string query = "CREATE (a:Account {peerId: $peerId, userName: $userName, createdDate: $createdDate})";
        var parameters = new
        {
            peerId = peer.PeerId.ToString(),
            userName = peer.UserName,
            createdDate = DateTime.UtcNow
        };

        await _session.RunAsync(query, parameters);
    }

    public async Task UpdatePeerAsync(PeerDto peer)
    {
        const string query = """
                                 MERGE (a:Account {peerId: $peerId})
                                 ON CREATE SET 
                                     a.userName = $userName,
                                     a.publicKey = $publicKey,
                                     a.createdDate = $modifiedDate
                                 ON MATCH SET 
                                     a.userName = $userName,
                                     a.publicKey = COALESCE($publicKey, a.publicKey),
                                     a.modifiedDate = $modifiedDate
                             """;

        var parameters = new
        {
            peerId = peer.PeerId.ToString(),
            userName = peer.UserName,
            publicKey = peer.PublicKey,
            modifiedDate = DateTime.UtcNow
        };

        await _session.ExecuteWriteAsync(async tx => { await tx.RunAsync(query, parameters); });
    }

    public async Task UnfriendAsync(PeerDto peer, PeerDto targetPeer)
    {
        await _session.ExecuteWriteAsync(async tx =>
        {
            // Remove the FRIENDS_WITH relationship in both directions
            const string query = """
                                  MATCH (a:Account {peerId: $peerId})-[r:FRIENDS_WITH]-(b:Account {peerId: $targetPeerId})
                                  DELETE r
                                 """;

            var parameters = new
            {
                peerId = peer.PeerId.ToString(),
                targetPeerId = targetPeer.PeerId.ToString()
            };

            var cursor = await tx.RunAsync(query, parameters);
            await cursor.ConsumeAsync();
            return true;
        });
    }

    public async Task SendFriendRequestAsync(PeerDto peer1, PeerDto peer2)
    {
        await _session.ExecuteWriteAsync(async tx =>
        {
            const string ensurePeersQuery = """
                MERGE (a1:Account {peerId: $peerId1})
                ON CREATE SET a1.userName = $userName1, a1.createdDate = $createdDate1
                MERGE (a2:Account {peerId: $peerId2})
                ON CREATE SET a2.userName = $userName2, a2.createdDate = $createdDate2
            """;

            var ensureParams = new
            {
                peerId1 = peer1.PeerId.ToString(),
                userName1 = peer1.UserName,
                createdDate1 = DateTime.UtcNow,
                peerId2 = peer2.PeerId.ToString(),
                userName2 = peer2.UserName,
                createdDate2 = DateTime.UtcNow
            };

            await tx.RunAsync(ensurePeersQuery, ensureParams);

            const string requestQuery = """
                MATCH (a1:Account {peerId: $peerId1}), (a2:Account {peerId: $peerId2})
                MERGE (a1)-[r:FRIEND_REQUEST]->(a2)
                ON CREATE SET r.createdDate = datetime()
            """;

            var requestParams = new
            {
                peerId1 = peer1.PeerId.ToString(),
                peerId2 = peer2.PeerId.ToString()
            };

            var cursor = await tx.RunAsync(requestQuery, requestParams);
            await cursor.ConsumeAsync();
            return true;
        });

        await hubContext.FriendRequestReceived(peer1, peer2);
    }

    public async Task AcceptFriendRequestAsync(PeerDto peer1, PeerDto peer2)
    {
        await _session.ExecuteWriteAsync(async tx =>
        {
            const string query = """
                                     MATCH (a1:Account {peerId: $peerId1})-[r:FRIEND_REQUEST]->(a2:Account {peerId: $peerId2})
                                     DELETE r
                                     MERGE (a1)-[:FRIENDS_WITH]->(a2)
                                     MERGE (a2)-[:FRIENDS_WITH]->(a1)
                                 """;

            var parameters = new { peerId1 = peer2.PeerId.ToString(), peerId2 = peer1.PeerId.ToString() };

            var cursor = await tx.RunAsync(query, parameters);
            await cursor.ConsumeAsync();

            return true;
        });
        await hubContext.FriendRequestAccepted(peer1, peer2);
    }

    public async Task<PeerResponse[]> GetFriendsAsync(Guid peerId)
    {
        const string query = """
                                 MATCH (a:Account {peerId: $peerId})-[:FRIENDS_WITH]-(friend:Account)
                                 RETURN friend.peerId, friend.userName, friend.createdDate, friend.publicKey
                             """;

        var parameters = new { peerId = peerId.ToString() };

        var result = await _session.RunAsync(query, parameters);
        return [.. (await result.ToListAsync(record => new PeerResponse(
                Guid.Parse(record["friend.peerId"].As<string>()),
                record["friend.userName"].As<string>(),
                record["friend.createdDate"].As<ZonedDateTime>().UtcDateTime,
                record["friend.publicKey"].As<string?>()
            )))
            .DistinctBy(a => a.PeerId)];
    }

    public async Task<PeerResponse[]> GetFriendRequestsAsync(Guid peerId)
    {
        const string query = """
                                 MATCH (sender:Account)-[:FRIEND_REQUEST]->(recipient:Account {peerId: $peerId})
                                 RETURN sender.peerId AS peerId, sender.userName AS userName, sender.createdDate AS createdDate, sender.publicKey AS publicKey
                             """;

        var parameters = new { peerId = peerId.ToString() };

        var result = await _session.RunAsync(query, parameters);

        return [.. (await result.ToListAsync(record => new PeerResponse(
                Guid.Parse(record["peerId"].As<string>()),
                record["userName"].As<string>(),
                record["createdDate"].As<ZonedDateTime>().UtcDateTime,
                record["publicKey"].As<string?>()
            )))];
    }

    public async Task<PeerResponse[]> GetRequestsISent(Guid peerId)
    {
        const string query = """
                                 MATCH (requester:Account {peerId: $peerId})-[:FRIEND_REQUEST]->(recipient:Account)
                                 RETURN recipient.peerId AS peerId, recipient.userName AS userName, recipient.createdDate AS createdDate, recipient.publicKey AS publicKey
                             """;

        var parameters = new { peerId = peerId.ToString() };

        var result = await _session.RunAsync(query, parameters);

        return [.. (await result.ToListAsync(record => new PeerResponse(
                Guid.Parse(record["peerId"].As<string>()),
                record["userName"].As<string>(),
                record["createdDate"].As<ZonedDateTime?>()!.UtcDateTime,
                record["publicKey"].As<string?>()
            )))];
    }

    private async Task<bool> PeerExistsAsync(Guid peerId)
    {
        const string query = "MATCH (a:Account {peerId: $peerId}) RETURN a";
        var parameters = new { peerId = peerId.ToString() };

        var result = await _session.RunAsync(query, parameters);
        return await result.FetchAsync();
    }
}