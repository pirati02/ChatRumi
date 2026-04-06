using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace ChatRum.InterCommunication;

public static class KafkaTopicBootstrap
{
    public const int DefaultPartitionCount = 6;
    public const short DefaultReplicationFactor = 1;

    /// <summary>
    /// Ensures account integration topics exist before consumers subscribe (avoids UNKNOWN_TOPIC_OR_PARTITION when no producer has run yet).
    /// </summary>
    public static async Task EnsureInterCommunicationTopicsAsync(
        string bootstrapServers,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        }).Build();

        TopicSpecification[] specs =
        [
            new TopicSpecification
            {
                Name = Topics.AccountUpdatedTopic,
                NumPartitions = DefaultPartitionCount,
                ReplicationFactor = DefaultReplicationFactor
            },
            new TopicSpecification
            {
                Name = Topics.AccountCreatedTopic,
                NumPartitions = DefaultPartitionCount,
                ReplicationFactor = DefaultReplicationFactor
            },
            new TopicSpecification
            {
                Name = Topics.NotificationTriggeredTopic,
                NumPartitions = DefaultPartitionCount,
                ReplicationFactor = DefaultReplicationFactor
            }
        ];

        try
        {
            await admin.CreateTopicsAsync(specs).ConfigureAwait(false);
        }
        catch (CreateTopicsException ex)
        {
            foreach (var result in ex.Results)
            {
                if (result.Error.Code is not ErrorCode.NoError and not ErrorCode.TopicAlreadyExists)
                {
                    throw new KafkaException(result.Error);
                }
            }
        }
    }
}
