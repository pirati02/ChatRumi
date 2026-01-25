using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace ChatRum.InterCommunication.Telemetry;

public static class RedisInstrumentationExtensions
{
    /// <summary>
    /// Adds Redis instrumentation to OpenTelemetry tracing.
    /// Call this after AddOpenTelemetryObservability() if your service uses Redis.
    /// </summary>
    public static TracerProviderBuilder AddRedisInstrumentationForConnection(
        this TracerProviderBuilder builder,
        IConnectionMultiplexer connection)
    {
        return builder.AddRedisInstrumentation(connection);
    }
}
