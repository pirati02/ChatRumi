namespace ChatRumi.Friendship.Application;

public record ApplicationOptions
{
    public const string Name = nameof(ApplicationOptions);
  
    public required string Neo4jConnection { get; set; }
    public required string Neo4jUser { get; set; }
    public required string Neo4jPassword { get; set; }
    public required string Neo4jDatabase { get; set; }
};