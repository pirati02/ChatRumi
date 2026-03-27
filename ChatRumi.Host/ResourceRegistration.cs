namespace ChatRumi.Host;

public static class ResourceRegistration
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ContainerResource> AddKafkaInternal()
        {
            var zookeeper = builder.AddContainer("zookeeper", "confluentinc/cp-zookeeper", "7.6.0")
                .WithEnvironment("ZOOKEEPER_CLIENT_PORT", "2181")
                .WithEnvironment("ZOOKEEPER_TICK_TIME", "2000")
                .WithEndpoint(port: 2181, targetPort: 2181, name: "client")
                .WithLifetime(ContainerLifetime.Persistent);

            var kafka = builder.AddContainer("kafka", "confluentinc/cp-kafka", "7.6.0")
                .WaitFor(zookeeper)
                .WithEndpoint(port: 9092, targetPort: 9092, name: "external")
                .WithEndpoint(port: 29092, targetPort: 29092, name: "internal")
                .WithEnvironment("KAFKA_BROKER_ID", "1")
                .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", "zookeeper:2181")
                .WithEnvironment("KAFKA_LISTENERS", "INTERNAL://0.0.0.0:29092,EXTERNAL://0.0.0.0:9092")
                .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "INTERNAL://kafka:29092,EXTERNAL://localhost:9092")
                .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "INTERNAL:PLAINTEXT,EXTERNAL:PLAINTEXT")
                .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "INTERNAL")
                .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
                .WithLifetime(ContainerLifetime.Persistent);

            builder.AddContainer("kafka-ui", "provectuslabs/kafka-ui", "latest")
                .WaitFor(kafka)
                .WithHttpEndpoint(port: 8082, targetPort: 8080, name: "ui")
                .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "local-kafka")
                .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka:29092")
                .WithEnvironment("KAFKA_CLUSTERS_0_ZOOKEEPER", "zookeeper:2181")
                .WithLifetime(ContainerLifetime.Persistent);

            return kafka;
        }

        public IResourceBuilder<RabbitMQServerResource> AddRabbitMqInternal(string defaultUser = "admin", string defaultPassword = "rbadminpass")
        {
            var rabbitMq = builder.AddRabbitMQ(
                    "rabbitmq",
                    builder.AddParameter("rabbitmq-username", defaultUser),
                    builder.AddParameter("rabbitmq-password", defaultPassword)
                )
                .WithManagementPlugin()
                .WithVolume("rabbitmq_data", "/var/lib/rabbitmq")
                .WithLifetime(ContainerLifetime.Persistent);
            
            return rabbitMq;
        }

        public IResourceBuilder<ElasticsearchResource> AddElasticsearchInternal()
        {
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

            return elastic;
        }

        public IResourceBuilder<ContainerResource> AddNeo4JInternal()
        {
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

            return neo4J;
        }
    }
}
