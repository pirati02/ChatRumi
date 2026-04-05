using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Text;

namespace ChatRum.InterCommunication.Telemetry;

/// <summary>
/// Middleware to capture HTTP request and response bodies for OpenTelemetry tracing
/// </summary>
public class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    int maxBodySize = 4096,
    params string[] excludedPaths)
{
    private readonly HashSet<string> _excludedPaths = new(excludedPaths, StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;

        // Skip if no activity or path is excluded
        if (activity == null || _excludedPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            await next(context);
            return;
        }

        // Capture request body
        await CaptureRequestBodyAsync(context, activity);

        // Capture response body
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await next(context);

            // Capture response details
            await CaptureResponseBodyAsync(context, activity, responseBody);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task CaptureRequestBodyAsync(HttpContext context, Activity activity)
    {
        if (context.Request.ContentLength > 0 && context.Request.ContentLength <= maxBodySize)
        {
            context.Request.EnableBuffering();

            try
            {
                var buffer = new byte[context.Request.ContentLength.Value];
                await context.Request.Body.ReadExactlyAsync(buffer);
                context.Request.Body.Position = 0;

                var requestBody = Encoding.UTF8.GetString(buffer);

                // Only log if it's text-based content
                if (IsTextBasedContentType(context.Request.ContentType))
                {
                    var safeBody = context.Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true
                        ? TelemetryRedaction.RedactSensitiveJson(requestBody)
                        : requestBody;
                    activity.SetTag("http.request.body", TruncateIfNeeded(safeBody, maxBodySize));
                    activity.SetTag("http.request.body.size", buffer.Length);
                }
                else
                {
                    activity.SetTag("http.request.body", "[Binary Content]");
                    activity.SetTag("http.request.body.size", buffer.Length);
                }
            }
            catch (Exception ex)
            {
                activity.SetTag("http.request.body.error", ex.Message);
            }
        }
        else if (context.Request.ContentLength > maxBodySize)
        {
            activity.SetTag("http.request.body", $"[Body too large: {context.Request.ContentLength} bytes]");
            activity.SetTag("http.request.body.size", context.Request.ContentLength);
        }
    }

    private async Task CaptureResponseBodyAsync(HttpContext context, Activity activity, MemoryStream responseBody)
    {
        responseBody.Seek(0, SeekOrigin.Begin);

        if (responseBody.Length > 0 && responseBody.Length <= maxBodySize)
        {
            try
            {
                var buffer = new byte[responseBody.Length];
                await responseBody.ReadExactlyAsync(buffer, 0, buffer.Length);
                responseBody.Seek(0, SeekOrigin.Begin);

                var responseBodyText = Encoding.UTF8.GetString(buffer);

                // Only log if it's text-based content
                if (IsTextBasedContentType(context.Response.ContentType))
                {
                    var safeBody = context.Response.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true
                        ? TelemetryRedaction.RedactSensitiveJson(responseBodyText)
                        : responseBodyText;
                    activity.SetTag("http.response.body", TruncateIfNeeded(safeBody, maxBodySize));
                    activity.SetTag("http.response.body.size", buffer.Length);
                }
                else
                {
                    activity.SetTag("http.response.body", "[Binary Content]");
                    activity.SetTag("http.response.body.size", buffer.Length);
                }
            }
            catch (Exception ex)
            {
                activity.SetTag("http.response.body.error", ex.Message);
            }
        }
        else if (responseBody.Length > maxBodySize)
        {
            activity.SetTag("http.response.body", $"[Body too large: {responseBody.Length} bytes]");
            activity.SetTag("http.response.body.size", responseBody.Length);
        }
    }

    private static bool IsTextBasedContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        var textBasedTypes = new[]
        {
            "application/json",
            "application/xml",
            "text/",
            "application/x-www-form-urlencoded",
            "application/graphql"
        };

        return textBasedTypes.Any(type => contentType.Contains(type, StringComparison.OrdinalIgnoreCase));
    }

    private static string TruncateIfNeeded(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text[..maxLength] + "... [truncated]";
    }
}
