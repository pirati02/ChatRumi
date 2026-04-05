using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChatRum.InterCommunication.Telemetry;

/// <summary>
/// Redacts secrets from strings attached to OpenTelemetry (query strings, JSON bodies, URIs).
/// </summary>
public static class TelemetryRedaction
{
    private static readonly HashSet<string> SensitiveQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "access_token", "token", "refresh_token", "password", "code", "client_secret", "id_token"
    };

    private static readonly HashSet<string> SensitiveJsonKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "refresh_token", "access_token", "token", "client_secret", "secret", "id_token"
    };

    public static bool IsProductionLikeEnvironment()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return !string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Redacts sensitive query parameter values (e.g. access_token for SignalR).
    /// </summary>
    public static string? RedactQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return queryString;
        }

        var span = queryString.AsSpan();
        if (span.Length > 0 && span[0] == '?')
        {
            span = span[1..];
        }

        if (span.IsEmpty)
        {
            return queryString;
        }

        var parts = new List<string>();
        foreach (var segment in span.ToString().Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = segment.IndexOf('=');
            if (eq <= 0)
            {
                parts.Add(segment);
                continue;
            }

            var key = segment[..eq];
            var value = segment[(eq + 1)..];
            if (SensitiveQueryKeys.Contains(key))
            {
                parts.Add($"{key}=[REDACTED]");
            }
            else
            {
                parts.Add($"{key}={value}");
            }
        }

        var rebuilt = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        return rebuilt;
    }

    /// <summary>
    /// Removes sensitive query keys from an absolute HTTP(S) URI for tracing.
    /// </summary>
    public static string? RedactHttpUri(string? uriString)
    {
        if (string.IsNullOrEmpty(uriString) || !Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            return uriString;
        }

        if (string.IsNullOrEmpty(uri.Query))
        {
            return uriString;
        }

        var redactedQuery = RedactQueryString(uri.Query);
        var builder = new StringBuilder();
        builder.Append(uri.GetLeftPart(UriPartial.Path));
        builder.Append(redactedQuery ?? string.Empty);
        return builder.ToString();
    }

    /// <summary>
    /// Redacts known sensitive JSON property values (passwords, tokens).
    /// </summary>
    public static string RedactSensitiveJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return json;
            }

            RedactJsonNode(node);
            return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }
        catch (JsonException)
        {
            return "[Unparseable JSON; not logged]";
        }
    }

    private static void RedactJsonNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(kvp => kvp.Key).ToList())
                {
                    var val = obj[key];
                    if (val is JsonObject or JsonArray)
                    {
                        RedactJsonNode(val);
                    }
                    else if (SensitiveJsonKeys.Contains(key))
                    {
                        obj[key] = "[REDACTED]";
                    }
                }

                break;
            case JsonArray arr:
                foreach (var item in arr)
                {
                    RedactJsonNode(item);
                }

                break;
        }
    }
}
