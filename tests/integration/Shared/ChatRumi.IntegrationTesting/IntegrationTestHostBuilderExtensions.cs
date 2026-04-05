using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatRumi.IntegrationTesting;

/// <summary>
/// Avoids Windows Event Log during host teardown (Marten daemon / hosted services can log after EventLog is disposed).
/// </summary>
public static class IntegrationTestHostBuilderExtensions
{
    public static IWebHostBuilder ConfigureIntegrationTestLogging(this IWebHostBuilder builder)
    {
        return builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }
}
