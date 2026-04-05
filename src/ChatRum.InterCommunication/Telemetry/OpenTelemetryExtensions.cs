using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Builder;
using Npgsql;

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

                        // Enrich spans with request details
                        options.EnrichWithHttpRequest = (activity, httpRequest) =>
                        {
                            activity.SetTag("http.request.content_type", httpRequest.ContentType);
                            activity.SetTag("http.request.content_length", httpRequest.ContentLength);
                            activity.SetTag("http.request.query", TelemetryRedaction.RedactQueryString(httpRequest.QueryString.Value));

                            // Add request headers (excluding sensitive ones)
                            foreach (var header in httpRequest.Headers.Where(h =>
                                !h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) &&
                                !h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)))
                            {
                                activity.SetTag($"http.request.header.{header.Key.ToLowerInvariant()}", string.Join(", ", header.Value!));
                            }
                        };

                        // Enrich spans with response details
                        options.EnrichWithHttpResponse = (activity, httpResponse) =>
                        {
                            activity.SetTag("http.response.content_type", httpResponse.ContentType);
                            activity.SetTag("http.response.content_length", httpResponse.ContentLength);

                            // Add response headers
                            foreach (var header in httpResponse.Headers.Where(h =>
                                !h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)))
                            {
                                activity.SetTag($"http.response.header.{header.Key.ToLowerInvariant()}", string.Join(", ", header.Value!));
                            }
                        };

                        // Enrich with exception details
                        options.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("exception.type", exception.GetType().FullName);
                            activity.SetTag("exception.message", exception.Message);
                            if (!TelemetryRedaction.IsProductionLikeEnvironment())
                            {
                                activity.SetTag("exception.stacktrace", exception.StackTrace);
                            }

                            if (exception.InnerException != null)
                            {
                                activity.SetTag("exception.inner.type", exception.InnerException.GetType().FullName);
                                activity.SetTag("exception.inner.message", exception.InnerException.Message);
                            }
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;

                        // Enrich HTTP client requests
                        options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                        {
                            activity.SetTag("http.request.method", httpRequestMessage.Method.ToString());
                            activity.SetTag("http.request.uri", TelemetryRedaction.RedactHttpUri(httpRequestMessage.RequestUri?.ToString()));

                            if (httpRequestMessage.Content != null)
                            {
                                activity.SetTag("http.request.content.type", httpRequestMessage.Content.Headers.ContentType?.ToString());
                                activity.SetTag("http.request.content.length", httpRequestMessage.Content.Headers.ContentLength);
                            }

                            // Add request headers (excluding sensitive ones)
                            foreach (var header in httpRequestMessage.Headers.Where(h =>
                                !h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)))
                            {
                                activity.SetTag($"http.request.header.{header.Key.ToLowerInvariant()}", string.Join(", ", header.Value));
                            }
                        };

                        // Enrich HTTP client responses
                        options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                        {
                            activity.SetTag("http.response.status_code", (int)httpResponseMessage.StatusCode);
                            activity.SetTag("http.response.status_text", httpResponseMessage.ReasonPhrase);

                            activity.SetTag("http.response.content.type", httpResponseMessage.Content.Headers.ContentType?.ToString());
                            activity.SetTag("http.response.content.length", httpResponseMessage.Content.Headers.ContentLength);
                        };

                        // Enrich with exception details
                        options.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("exception.type", exception.GetType().FullName);
                            activity.SetTag("exception.message", exception.Message);
                            if (!TelemetryRedaction.IsProductionLikeEnvironment())
                            {
                                activity.SetTag("exception.stacktrace", exception.StackTrace);
                            }
                        };
                    })
                    .AddNpgsql() // Enhanced PostgreSQL tracing with SQL commands
                    .AddSource("Marten") // Marten (PostgreSQL) tracing
                    .AddSource("Npgsql") // PostgreSQL tracing
                    .AddSource("Confluent.Kafka") // Kafka tracing
                    .AddSource(telemetryOptions.ServiceName) // Custom service traces
                    .SetSampler(new AlwaysOnSampler()); // Ensure all traces are captured


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

    /// <summary>
    /// Adds middleware to capture request and response bodies for OpenTelemetry tracing
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="maxBodySize">Maximum body size to capture (default: 4096 bytes)</param>
    /// <param name="excludedPaths">Paths to exclude from body capture (e.g., /health, /metrics)</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseRequestResponseLogging(
        this IApplicationBuilder app,
        int maxBodySize = 4096,
        params string[] excludedPaths)
    {
        var defaultExcludedPaths = new[]
        {
            "/health", "/metrics", "/swagger",
            "/api/account/login", "/api/account/refresh"
        };
        var allExcludedPaths = excludedPaths.Length > 0
            ? defaultExcludedPaths.Concat(excludedPaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : defaultExcludedPaths;

        return app.UseMiddleware<RequestResponseLoggingMiddleware>(maxBodySize, allExcludedPaths);
    }
}
