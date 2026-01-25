namespace ChatRum.InterCommunication.Telemetry;

public class OpenTelemetryOptions
{
    public const string Name = "OpenTelemetry";
    
    public string ServiceName { get; set; } = "chatrumi-service";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string OtlpEndpoint { get; set; } = "http://jaeger:4317";
    public bool Enabled { get; set; } = true;
}
