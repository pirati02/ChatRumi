namespace ChatRumi.Host;

public static class ServiceRegistrations
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ProjectResource> AddAccountService(
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<RedisResource> redis,
            IResourceBuilder<RabbitMQServerResource> rabbitMq,
            string defaultUser,
            string defaultPassword
        )
        {
            var accountDatabase = postgres.AddDatabase("accountDatabase", "accountDatabase");

            return builder.AddProject<Projects.ChatRumi_Account_Api>("accountService")
                .WithHttpHealthCheck("/health")
                .WithReference(redis)
                .WithReference(rabbitMq)
                .WithReference(accountDatabase)
                .WaitFor(accountDatabase)
                .WaitFor(redis)
                .WaitFor(rabbitMq)
                .WithEnvironment("RedisOptions__Host", "redis")
                .WithEnvironment("RedisOptions__Port", "6379")
                .WithEnvironment("RedisOptions__User", "default")
                .WithEnvironment("RedisOptions__Password", "")
                .WithEnvironment("RedisOptions__Expiration", "2")
                .WithEnvironment("MassTransit_Url", rabbitMq.Resource.ConnectionStringExpression)
                .WithEnvironment("MassTransit_User", defaultUser)
                .WithEnvironment("MassTransit_Password", defaultPassword);
        }

        public IResourceBuilder<ProjectResource> AddChatService(
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<RedisResource> redis,
            IResourceBuilder<RabbitMQServerResource> rabbitMq,
            string defaultUser,
            string defaultPassword
        )
        {
            var chatDatabase = postgres.AddDatabase("chatDatabase", "chatDatabase");

            return builder.AddProject<Projects.ChatRumi_Chat_Api>("chatService")
                .WithHttpHealthCheck("/health")
                .WithReference(redis)
                .WithReference(rabbitMq)
                .WithReference(chatDatabase)
                .WaitFor(chatDatabase)
                .WaitFor(redis)
                .WaitFor(rabbitMq)
                .WithEnvironment("RedisOptions__Host", "redis")
                .WithEnvironment("RedisOptions__Port", "6379")
                .WithEnvironment("RedisOptions__User", "default")
                .WithEnvironment("RedisOptions__Password", "")
                .WithEnvironment("RedisOptions__Expiration", "2")
                .WithEnvironment("MassTransit_Url", rabbitMq.Resource.ConnectionStringExpression)
                .WithEnvironment("MassTransit_User", defaultUser)
                .WithEnvironment("MassTransit_Password", defaultPassword);
        }

        public IResourceBuilder<ProjectResource> AddFeedService(
            IResourceBuilder<ElasticsearchResource> elastic
        )
        {
            return builder.AddProject<Projects.ChatRumi_Feed_Api>("feedService")
                .WithHttpHealthCheck("/health")
                .WaitFor(elastic)
                .WithReference(elastic)
                .WithEnvironment("ConnectionStrings__FeedContext", elastic.Resource.ConnectionStringExpression);
        }

        public IResourceBuilder<ProjectResource> AddFriendshipService(
            IResourceBuilder<ContainerResource> neo4J
        )
        {
            return builder.AddProject<Projects.ChatRumi_Friendship_Api>("friendshipService")
                .WithHttpHealthCheck("/health")
                .WaitFor(neo4J)
                .WithEnvironment("Neo4jOptions__Neo4jConnection", "bolt://neo4j-dev:7687")
                .WithEnvironment("Neo4jOptions__Neo4jUser", "neo4j")
                .WithEnvironment("Neo4jOptions__Neo4jPassword", "Passw0rd")
                .WithEnvironment("Neo4jOptions__Neo4jDatabase", "neo4j");
        }
    }
}