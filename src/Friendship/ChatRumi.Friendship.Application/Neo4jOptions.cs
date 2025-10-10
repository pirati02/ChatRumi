namespace ChatRumi.Friendship.Application;

public record Neo4jOptions
{
    public const string Name = nameof(Neo4jOptions);
  
    public required string Neo4jConnection { get; set; }
    public required string Neo4jUser { get; set; }
    public required string Neo4jPassword { get; set; }
    public required string Neo4jDatabase { get; set; }
};