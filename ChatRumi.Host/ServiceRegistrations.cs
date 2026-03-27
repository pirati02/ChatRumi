namespace ChatRumi.Host;

public static class ServiceRegistrations
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IDistributedApplicationBuilder AddAccountService(
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<RedisResource> redis,
            IResourceBuilder<RabbitMQServerResource> rabbitMq,
            string defaultUser,
            string defaultPassword
        )
        {
            var accountDatabase = postgres.AddDatabase("accountDatabase", "accountDatabase");
            
            builder.AddProject<Projects.ChatRumi_Account_Api>("accountService")
                .WithHttpHealthCheck("/health")
                .WithReference(redis)
                .WithReference(accountDatabase)
                .WaitFor(accountDatabase)
                .WaitFor(redis)
                .WithEnvironment("ConnectionStrings__Marten", accountDatabase.Resource.ConnectionStringExpression.ValueExpression)
                .WithEnvironment("RedisOptions__Host", "redis")
                .WithEnvironment("RedisOptions__Port", "6379")
                .WithEnvironment("RedisOptions__User", "default")
                .WithEnvironment("RedisOptions__Password", "")
                .WithEnvironment("RedisOptions__Expiration", "2")
                .WithEnvironment("MassTransit_Url", rabbitMq.Resource.ConnectionStringExpression.ValueExpression)
                .WithEnvironment("MassTransit_User", defaultUser)
                .WithEnvironment("MassTransit_Password", defaultPassword);
            
            return builder;
        }
        
        public IDistributedApplicationBuilder AddChatService(
            IResourceBuilder<PostgresServerResource> postgres,
            IResourceBuilder<RedisResource> redis,
            IResourceBuilder<RabbitMQServerResource> rabbitMq,
            string defaultUser,
            string defaultPassword
        )
        {
            var chatDatabase = postgres.AddDatabase("chatDatabase", "chatDatabase");

            builder.AddProject<Projects.ChatRumi_Chat_Api>("chatService")
                .WithHttpHealthCheck("/health")
                .WithReference(redis)
                .WithReference(chatDatabase)
                .WaitFor(chatDatabase)
                .WaitFor(redis)
                .WithEnvironment("ConnectionStrings__Marten", chatDatabase.Resource.ConnectionStringExpression.ValueExpression)
                .WithEnvironment("RedisOptions__Host", "redis")
                .WithEnvironment("RedisOptions__Port", "6379")
                .WithEnvironment("RedisOptions__User", "default")
                .WithEnvironment("RedisOptions__Password", "")
                .WithEnvironment("RedisOptions__Expiration", "2")
                .WithEnvironment("MassTransit_Url", rabbitMq.Resource.ConnectionStringExpression.ValueExpression)
                .WithEnvironment("MassTransit_User", defaultUser)
                .WithEnvironment("MassTransit_Password", defaultPassword);
            
            return builder;
        }
    }
}