using System.Text.Json;
using System.Text.Json.Serialization;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Dto;

public class MessageContentConverter : JsonConverter<MessageContent>
{
    private const string LegacyEncryptedPlaceholder = "[Encrypted message — not available]";

    public override MessageContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Check for type discriminator with multiple possible property names
        string? type = null;
        if (root.TryGetProperty("$type", out var typeProp) ||
            root.TryGetProperty("type", out typeProp))
        {
            type = typeProp.GetString();
        }

        // Get content value - try both camelCase and PascalCase
        string content = "";
        if (root.TryGetProperty("content", out var contentProp) ||
            root.TryGetProperty("Content", out contentProp))
        {
            content = contentProp.GetString() ?? "";
        }

        // Default to plain text if no type discriminator
        return type switch
        {
            "plain" or null => new PlainTextContent { Content = content },
            "link" => new LinkContent { Content = content },
            "image" => new ImageContent { Content = content },
            "encrypted" => new PlainTextContent { Content = LegacyEncryptedPlaceholder },
            _ => new PlainTextContent { Content = content } // fallback
        };
    }

    public override void Write(Utf8JsonWriter writer, MessageContent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var type = value switch
        {
            PlainTextContent => "plain",
            LinkContent => "link",
            ImageContent => "image",
            _ => "plain"
        };
        writer.WriteString("type", type);
        writer.WriteString("content", value.Content);

        writer.WriteEndObject();
    }
}
