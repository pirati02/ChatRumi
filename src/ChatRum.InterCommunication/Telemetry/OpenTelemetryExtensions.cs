using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace ChatRum.InterCommunication.Telemetry;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var telemetryOptions = configuration.GetSection(OpenTelemetryOptions.Name)
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        if (!telemetryOptions.Enabled)
        {
            return services;
        }

        // Configure OpenTelemetry with tracing and metrics
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: telemetryOptions.ServiceName,
                    serviceVersion: telemetryOptions.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddSource("Marten") // Marten (PostgreSQL) tracing
                    .AddSource("Npgsql") // PostgreSQL tracing
                    .AddSource("Confluent.Kafka") // Kafka tracing
                    .AddSource(telemetryOptions.ServiceName); // Custom service traces


                // Export to OTLP (Jaeger)
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(telemetryOptions.OtlpEndpoint);
                });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(telemetryOptions.ServiceName);

                // Export to OTLP (Jaeger)
                metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(telemetryOptions.OtlpEndpoint);
                });
            });

        return services;
    }
}
