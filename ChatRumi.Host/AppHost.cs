using ChatRumi.Host;

var builder = DistributedApplication.CreateBuilder(args);

const string defaultUser = "admin";
const string defaultPassword = "rbadminpass";
var rabbitMq = builder.AddRabbitMQ(
        "rabbitmq",
        builder.AddParameter("rabbitmq-username", defaultUser),
        builder.AddParameter("rabbitmq-password", defaultPassword)
    )
    .WithManagementPlugin()
    .WithVolume("rabbitmq_data", "/var/lib/rabbitmq")
    .WithLifetime(ContainerLifetime.Persistent);

var redis = builder.AddRedis("redis")
    .WithVolume("redis_data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

var postgres = builder.AddPostgres(
    "postgres",
    builder.AddParameter("postgres-password", "postgres_pass"),
    builder.AddParameter("postgres-username", "postgres_user")
).WithLifetime(ContainerLifetime.Persistent);

var elastic = builder.AddElasticsearch(
        "elastic",
        builder.AddParameter("elastic-password", "elastic_pass")
    )
    .WithEnvironment("xpack.security.enabled", "false")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddContainer("kibana", "docker.elastic.co/kibana/kibana", "8.15.3")
    .WithEndpoint(targetPort: 5601, name: "kibana-ui")
    .WaitFor(elastic)
    .WithEnvironment("ELASTICSEARCH_HOSTS", elastic.GetEndpoint("http"))
    .WithLifetime(ContainerLifetime.Persistent);

var neo4J = builder.AddContainer("neo4j-dev", "neo4j:latest")
    .WithEndpoint(targetPort: 7474, name: "neo4j-http")
    .WithEndpoint(targetPort: 7687, name: "neo4j-bolt")
    .WithEnvironment("NEO4J_AUTH", "neo4j/Passw0rd")
    .WithEnvironment("NEO4J_apoc_export_file_enabled", "true")
    .WithEnvironment("NEO4J_apoc_import_file_enabled", "true")
    .WithEnvironment("NEO4J_apoc_import_file_use__neo4j__config", "true")
    .WithEnvironment("NEO4J_PLUGINS", "[\"apoc\", \"graph-data-science\"]")
    .WithVolume("neo4j_data", "/data")
    .WithVolume("neo4j_logs", "/logs")
    .WithVolume("neo4j_import", "/var/lib/neo4j/import")
    .WithVolume("neo4j_plugins", "/plugins")
    .WithLifetime(ContainerLifetime.Persistent);

var accountService = builder.AddAccountService(postgres, redis, rabbitMq, defaultUser, defaultPassword);
var chatService = builder.AddChatService(postgres, redis, rabbitMq, defaultUser, defaultPassword);
var feedService = builder.AddFeedService(elastic);
var friendshipService = builder.AddFriendshipService(neo4J);

builder.AddProject<Projects.ChatRum_Gateway>("gateway")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Local")
    .WithReference(accountService)
    .WithReference(chatService)
    .WithReference(feedService)
    .WithReference(friendshipService)
    .WaitFor(accountService)
    .WaitFor(chatService)
    .WaitFor(feedService)
    .WaitFor(friendshipService)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();