using ChatRumi.Host;

var builder = DistributedApplication.CreateBuilder(args);

const string defaultUser = "admin";
const string defaultPassword = "rbadminpass";
var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin(port: 15672)
    .WithEndpoint(port: 5672, targetPort: 5672, name: "amqp")
    .WithContainerRuntimeArgs(
        "--health-cmd=rabbitmq-diagnostics -q ping",
        "--health-interval=10s",
        "--health-timeout=5s",
        "--health-retries=12",
        "--health-start-period=20s"
    )
    .WithEnvironment("RABBITMQ_DEFAULT_USER", defaultUser)
    .WithEnvironment("RABBITMQ_DEFAULT_PASS", defaultPassword)
    .WithVolume("rabbitmq_data", "/var/lib/rabbitmq");

var redis = builder.AddRedis("redis")
    .WithVolume("redis_data", "/data");

var postgres = builder.AddPostgres(
    "postgres",
    builder.AddParameter("postgres-password", "postgres_pass"),
    builder.AddParameter("postgres-username", "postgres_user")
);

var elastic = builder.AddElasticsearch(
    "elastic",
    builder.AddParameter("elastic-password", "elastic_pass")
).WithEnvironment("xpack.security.enabled", "false");
builder.AddContainer("kibana", "docker.elastic.co/kibana/kibana", "8.15.3")
    .WithEndpoint(targetPort: 5601, name: "kibana-ui")
    .WaitFor(elastic)
    .WithEnvironment("ELASTICSEARCH_HOSTS", elastic.GetEndpoint("http"));
 
builder.AddAccountService(postgres, redis, rabbitMq, defaultUser, defaultPassword)
    .AddChatService(postgres, redis, rabbitMq, defaultUser, defaultPassword);

builder.AddProject<Projects.ChatRumi_Feed_Api>("feedService")
    .WithHttpHealthCheck("/health")
    .WaitFor(elastic)
    .WithReference(elastic)
    .WithEnvironment("ConnectionStrings__FeedContext", elastic.Resource.ConnectionStringExpression.ValueExpression);

var friendshipService = builder.AddProject<Projects.ChatRumi_Friendship_Api>("friendshipService")
    .WithHttpHealthCheck("/health");

var gateway = builder.AddProject<Projects.ChatRum_Gateway>("gateway")
    .WithHttpHealthCheck("/health");

builder.Build().Run();