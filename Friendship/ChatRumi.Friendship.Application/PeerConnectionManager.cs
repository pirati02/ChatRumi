using ChatRumi.Friendship.Domain.Aggregates;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace ChatRumi.Friendship.Application;

public interface IPeerConnectionManager
{
    Task CreatePeerAsync(Guid peerId, string userName, DateTime createdDate);
    Task<bool> PeerExistsAsync(Guid peerId);
    Task SendFriendRequestAsync(Guid peerId1, Guid peerId2);
    Task AcceptFriendRequestAsync(Guid peerId1, Guid peerId2);
    Task<IEnumerable<Peer>> GetFriendsAsync(Guid peerId);
}

public class PeerConnectionManager : IPeerConnectionManager
{
    private readonly IAsyncSession _session;
    public PeerConnectionManager(
        IDriver driver,
        IOptions<ApplicationOptions> options
    )
    {
        _session = driver.AsyncSession(builder => builder.WithDatabase(options.Value.Neo4jDatabase));
    }
    
    public async Task CreatePeerAsync(Guid peerId, string userName, DateTime createdDate)
    {
        const string query = "CREATE (a:Account {peerId: $peerId, userName: $userName, createdDate: $createdDate})";
        var parameters = new { peerId = peerId.ToString(), userName, createdDate };

        await _session.RunAsync(query, parameters);
    }
    
    public async Task<bool> PeerExistsAsync(Guid peerId)
    {
        const string query = "MATCH (a:Account {peerId: $peerId}) RETURN a";
        var parameters = new { peerId = peerId.ToString() };

        var result = await _session.RunAsync(query, parameters);
        return await result.FetchAsync();
    }
    
    public async Task SendFriendRequestAsync(Guid peerId1, Guid peerId2)
    {
        const string query = """
                                 MATCH (a1:Account {peerId: $peerId1}), (a2:Account {peerId: $peerId2})
                                 CREATE (a1)-[:FRIEND_REQUEST {status: 'pending', sentAt: datetime()}]->(a2)
                             """;

        var parameters = new { peerId1 = peerId1.ToString(), peerId2 = peerId2.ToString() };

        await _session.RunAsync(query, parameters);
    }

    public async Task AcceptFriendRequestAsync(Guid peerId1, Guid peerId2)
    {
        const string query = """
                                 MATCH (a1:Account {peerId: $peerId1})-[r:FRIEND_REQUEST {status: 'pending'}]->(a2:Account {peerId: $peerId2})
                                 DELETE r
                                 CREATE (a1)-[:FRIENDS_WITH]->(a2)
                                 CREATE (a2)-[:FRIENDS_WITH]->(a1)
                             """;

        var parameters = new { peerId1 = peerId1.ToString(), peerId2 = peerId2.ToString() };

        await _session.RunAsync(query, parameters);
    }

    public async Task<IEnumerable<Peer>> GetFriendsAsync(Guid peerId)
    {
        const string query = """
                                 MATCH (a:Account {peerId: $peerId})-[:FRIENDS_WITH]-(friend:Account)
                                 RETURN friend.peerId, friend.userName, friend.createdDate
                             """;

        var parameters = new { peerId = peerId.ToString() };
        var friends = new List<Peer>();

        var result = await _session.RunAsync(query, parameters);
        await result.ForEachAsync(record =>
        {
            var peer = new Peer
            {
                Id = Guid.Parse(record["friend.peerId"].As<string>()),
                UserName = record["friend.userName"].As<string>(),
                CreatedDate = record["friend.createdDate"].As<DateTime>()
            };
            friends.Add(peer);
        });

        return friends;
    }
}