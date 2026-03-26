namespace ChatRum.InterCommunication.Telemetry;

/// <summary>
/// Helper class to configure Npgsql for OpenTelemetry tracing with SQL command text
/// </summary>
public static class NpgsqlTelemetryHelper
{
    /// <summary>
    /// Configures a connection string to enable SQL command text capture in OpenTelemetry traces
    /// </summary>
    /// <param name="connectionString">The original connection string</param>
    /// <returns>Connection string with telemetry enabled</returns>
    public static string EnableTelemetry(string connectionString)
    {
        // Npgsql.OpenTelemetry automatically captures SQL command text when integrated
        // Ensure Include Error Detail is enabled for better error tracing
        if (!connectionString.Contains("Include Error Detail", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = connectionString.TrimEnd(';') + ";Include Error Detail=true";
        }

        return connectionString;
    }
}
