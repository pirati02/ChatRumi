using Confluent.Kafka;

namespace ChatRum.InterCommunication;

public record KafkaOptions
{
    public const string Name = nameof(KafkaOptions);

    public required string ConnectionString { get; set; }

    /// <summary>Producer compression (e.g. lz4, snappy, gzip, none).</summary>
    public string CompressionType { get; set; } = "lz4";

    /// <summary>Milliseconds to wait for batching before sending.</summary>
    public double LingerMs { get; set; } = 5;

    /// <summary>Maximum size of a single batch in bytes.</summary>
    public int BatchSizeBytes { get; set; } = 65536;

    /// <summary>
    /// Durability vs latency: <see cref="Acks.Leader"/> (acks=1) is typical for dev;
    /// <see cref="Acks.All"/> for maximum durability when replicas exist.
    /// </summary>
    public Acks ProducerAcks { get; set; } = Acks.Leader;
}
