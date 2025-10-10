namespace ChatRum.InterCommunication;

public record KafkaOptions
{
    public const string Name = nameof(KafkaOptions);
    public required string ConnectionString { get; set; }
};