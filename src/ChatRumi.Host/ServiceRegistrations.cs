namespace ChatRumi.Host;

public static class ServiceRegistrations
{
    /// <summary>Launch profile name defined in each downstream project's <c>launchSettings.json</c> (HTTPS + HTTP URLs).</summary>
    private const string HttpsLaunchProfile = "https";

    /// <summary>
    /// Aspire Redis primary endpoint <c>tcp</c> may be TLS (<c>rediss://</c>). This app uses StackExchange.Redis
    /// without SSL, so we inject the plain-TCP <c>secondary</c> endpoint (see RedisResource.SecondaryEndpointName).
    /// </summary>
    private const string RedisPlainTcpEndpointName = "secondary";

    private static EndpointReferenceExpression RedisPlainTcpHost(IResourceBuilder<RedisResource> redis) =>
        redis.GetEndpoint(RedisPlainTcpEndpointName).Property(EndpointProperty.Host);

    private static EndpointReferenceExpression RedisPlainTcpPort(IResourceBuilder<RedisResource> redis) =>
        redis.GetEndpoint(RedisPlainTcpEndpointName).Property(EndpointProperty.Port);

    private static ReferenceExpression RedisPassword(IResourceBuilder<RedisResource> redis) =>
        ReferenceExpression.Create($"{redis.Resource.PasswordParameter!}");

    private static ReferenceExpression KafkaBootstrapServers(IResourceBuilder<ContainerResource> kafka)
    {
        var endpoint = kafka.GetEndpoint("external");
        return ReferenceExpression.Create(
            $"{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)}");
    }

    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ProjectResource> AddAccountService(
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<RedisResource> redis,
            IResourceBuilder<RabbitMQServerResource> rabbitMq,
            IResourceBuilder<ContainerResource> kafka,
            string defaultUser,
            string defaultPassword
        )
        {
            var accountDatabase = postgres.AddDatabase("accountDatabase", "accountDatabase");

            return builder.AddProject<Projects.ChatRumi_Account_Api>("accountService", HttpsLaunchProfile)
                .WithHttpHealthCheck("/health")
                .WithReference(redis)
                .WithReference(rabbitMq)
                .WithReference(accountDatabase)
                .WaitFor(accountDatabase)
                .WaitFor(redis)
                .WaitFor(rabbitMq)
                .WaitFor(kafka)
                .WithEnvironment("KafkaOptions__ConnectionString", KafkaBootstrapServers(kafka))
                .WithEnvironment("RedisOptions__Host", RedisPlainTcpHost(redis))
                .WithEnvironment("RedisOptions__Port", RedisPlainTcpPort(redis))
                .WithEnvironment("RedisOptions__User", "default")
                .WithEnvironment("RedisOptions__Password", RedisPassword(redis))
                .WithEnvironment("RedisOptions__Expiration", "2")
                .WithEnvironment("MassTransit_Url", rabbitMq.Resource.ConnectionStringExpression)
                .WithEnvironment("MassTransit_User", defaultUser)
                .WithEnvironment("MassTransit_Password", defaultPassword);
        }

        public IResourceBuilder<ProjectResource> AddChatService(
            IResourceBuilder<PostgresDatabaseResource> chatDatabase,
            IResourceBuilder<RedisResource> redis,
            IResourceBuilder<RabbitMQServerResource> rabbitMq,
            IResourceBuilder<ProjectResource> accountService,
            string defaultUser,
            string defaultPassword
        )
        {
            return builder.AddProject<Projects.ChatRumi_Chat_Api>("chatService", HttpsLaunchProfile)
                .WithHttpHealthCheck("/health")
                .WithReference(redis)
                .WithReference(rabbitMq)
                .WithReference(chatDatabase)
                .WithReference(accountService)
                .WaitFor(chatDatabase)
                .WaitFor(redis)
                .WaitFor(rabbitMq)
                .WaitFor(accountService)
                .WithEnvironment("RedisOptions__Host", RedisPlainTcpHost(redis))
                .WithEnvironment("RedisOptions__Port", RedisPlainTcpPort(redis))
                .WithEnvironment("RedisOptions__User", "default")
                .WithEnvironment("RedisOptions__Password", RedisPassword(redis))
                .WithEnvironment("RedisOptions__Expiration", "2")
                .WithEnvironment("MassTransit_Url", rabbitMq.Resource.ConnectionStringExpression)
                .WithEnvironment("MassTransit_User", defaultUser)
                .WithEnvironment("MassTransit_Password", defaultPassword);
        }

        public IResourceBuilder<ProjectResource> AddChatAccountSyncService(
            IResourceBuilder<PostgresDatabaseResource> chatDatabase,
            IResourceBuilder<RedisResource> redis,
            IResourceBuilder<RabbitMQServerResource> rabbitMq,
            IResourceBuilder<ContainerResource> kafka,
            string defaultUser,
            string defaultPassword
        )
        {
            return builder.AddProject<Projects.ChatRumi_Chat_AccountSync>("chatAccountSyncService", HttpsLaunchProfile)
                .WithHttpHealthCheck("/health")
                .WithReference(redis)
                .WithReference(rabbitMq)
                .WithReference(chatDatabase)
                .WaitFor(chatDatabase)
                .WaitFor(redis)
                .WaitFor(rabbitMq)
                .WaitFor(kafka)
                .WithEnvironment("KafkaOptions__ConnectionString", KafkaBootstrapServers(kafka))
                .WithEnvironment("RedisOptions__Host", RedisPlainTcpHost(redis))
                .WithEnvironment("RedisOptions__Port", RedisPlainTcpPort(redis))
                .WithEnvironment("RedisOptions__User", "default")
                .WithEnvironment("RedisOptions__Password", RedisPassword(redis))
                .WithEnvironment("RedisOptions__Expiration", "2")
                .WithEnvironment("MassTransit_Url", rabbitMq.Resource.ConnectionStringExpression)
                .WithEnvironment("MassTransit_User", defaultUser)
                .WithEnvironment("MassTransit_Password", defaultPassword);
        }

        public IResourceBuilder<ProjectResource> AddFeedAccountSyncService(
            IResourceBuilder<ElasticsearchResource> elastic, 
            IResourceBuilder<ContainerResource> kafka
        )
        {
            return builder.AddProject<Projects.ChatRumi_Feed_AccountSync>("feedAccountSyncService", HttpsLaunchProfile)
              .WithHttpHealthCheck("/health")
              .WaitFor(elastic)
              .WaitFor(kafka)
              .WithReference(elastic)
              .WithEnvironment("ConnectionStrings__FeedContext", elastic.Resource.ConnectionStringExpression)
              .WithEnvironment("KafkaOptions__ConnectionString", KafkaBootstrapServers(kafka));
        }

        public IResourceBuilder<ProjectResource> AddFeedService(
            IResourceBuilder<ElasticsearchResource> elastic
        )
        {
            return builder.AddProject<Projects.ChatRumi_Feed_Api>("feedService", HttpsLaunchProfile)
                .WithHttpHealthCheck("/health")
                .WaitFor(elastic)
                .WithReference(elastic)
                .WithEnvironment("ConnectionStrings__FeedContext", elastic.Resource.ConnectionStringExpression);
        }

        public IResourceBuilder<ProjectResource> AddNotificationService(
            IResourceBuilder<ElasticsearchResource> elastic,
            IResourceBuilder<ContainerResource> kafka
        )
        {
            return builder.AddProject<Projects.ChatRumi_Notification_Api>("notificationService", HttpsLaunchProfile)
                .WithHttpHealthCheck("/health")
                .WaitFor(elastic)
                .WaitFor(kafka)
                .WithReference(elastic)
                .WithEnvironment("ConnectionStrings__FeedContext", elastic.Resource.ConnectionStringExpression)
                .WithEnvironment("KafkaOptions__ConnectionString", KafkaBootstrapServers(kafka));
        }

        public IResourceBuilder<ProjectResource> AddFriendshipService(
            IResourceBuilder<ContainerResource> neo4J
        )
        {
            var bolt = neo4J.GetEndpoint("neo4j-bolt");
            var neo4jUri = ReferenceExpression.Create(
                $"bolt://{bolt.Property(EndpointProperty.Host)}:{bolt.Property(EndpointProperty.Port)}");

            return builder.AddProject<Projects.ChatRumi_Friendship_Api>("friendshipService", HttpsLaunchProfile)
                .WithHttpHealthCheck("/health")
                .WaitFor(neo4J)
                .WithEnvironment("Neo4jOptions__Neo4jConnection", neo4jUri)
                .WithEnvironment("Neo4jOptions__Neo4jUser", "neo4j")
                .WithEnvironment("Neo4jOptions__Neo4jPassword", "Passw0rd")
                .WithEnvironment("Neo4jOptions__Neo4jDatabase", "neo4j");
        }

        public IResourceBuilder<ProjectResource> AddFriendshipAccountSyncService(
            IResourceBuilder<ContainerResource> neo4J,
            IResourceBuilder<ContainerResource> kafka
        )
        {
            var bolt = neo4J.GetEndpoint("neo4j-bolt");
            var neo4jUri = ReferenceExpression.Create(
                $"bolt://{bolt.Property(EndpointProperty.Host)}:{bolt.Property(EndpointProperty.Port)}");

            return builder.AddProject<Projects.ChatRumi_Friendship_AccountSync>("friendshipAccountSyncService", HttpsLaunchProfile)
                .WithHttpHealthCheck("/health")
                .WaitFor(neo4J)
                .WaitFor(kafka)
                .WithEnvironment("Neo4jOptions__Neo4jConnection", neo4jUri)
                .WithEnvironment("Neo4jOptions__Neo4jUser", "neo4j")
                .WithEnvironment("Neo4jOptions__Neo4jPassword", "Passw0rd")
                .WithEnvironment("Neo4jOptions__Neo4jDatabase", "neo4j")
                .WithEnvironment("KafkaOptions__ConnectionString", KafkaBootstrapServers(kafka));
        }
    }
}