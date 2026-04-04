using ChatRumi.Host;

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafkaInternal();

const string defaultUser = "admin";
const string defaultPassword = "rbadminpass";
var rabbitMq = builder.AddRabbitMqInternal(defaultUser, defaultPassword);

var elastic = builder.AddElasticsearchInternal();

var redisPassword = builder.AddParameter("redis-password", "redis_dev_password");
var redis = builder.AddRedis("redis")
    .WithPassword(redisPassword)
    .WithVolume("redis_data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

var postgres = builder.AddPostgres(
    "postgres",
    builder.AddParameter("postgres-password", "postgres_pass"),
    builder.AddParameter("postgres-username", "postgres_user")
).WithLifetime(ContainerLifetime.Persistent);

var neo4J = builder.AddNeo4JInternal();

var jwtSigningKey = builder.AddParameter("jwt-signing-key", "local_dev_jwt_signing_key_at_least_32_chars");

var accountService = builder.AddAccountService(postgres, redis, rabbitMq, defaultUser, defaultPassword)
    .WithChatRumiJwt(jwtSigningKey);

var chatDatabase = postgres.AddDatabase("chatDatabase", "chatDatabase");
var chatService = builder.AddChatService(chatDatabase, redis, rabbitMq, accountService, defaultUser, defaultPassword)
    .WithChatRumiJwt(jwtSigningKey);
builder.AddChatAccountSyncService(chatDatabase, redis, rabbitMq, kafka, defaultUser, defaultPassword);

builder.AddFeedAccountSyncService(elastic, kafka);
builder.AddFriendshipAccountSyncService(neo4J, kafka);

var feedService = builder.AddFeedService(elastic)
    .WithChatRumiJwt(jwtSigningKey);
var friendshipService = builder.AddFriendshipService(neo4J)
    .WithChatRumiJwt(jwtSigningKey);

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