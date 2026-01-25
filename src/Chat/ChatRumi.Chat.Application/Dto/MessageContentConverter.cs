using System.Text.Json;
using System.Text.Json.Serialization;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Dto;

public class MessageContentConverter : JsonConverter<MessageContent>
{
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
            "encrypted" => DeserializeEncryptedContent(root, content),
            _ => new PlainTextContent { Content = content } // fallback
        };
    }

    private static EncryptedContent DeserializeEncryptedContent(JsonElement root, string content)
    {
        var iv = "";
        if (root.TryGetProperty("iv", out var ivProp) ||
            root.TryGetProperty("Iv", out ivProp))
        {
            iv = ivProp.GetString() ?? "";
        }

        var encryptedKeys = new Dictionary<Guid, string>();
        if (!root.TryGetProperty("encryptedKeys", out var keysProp) &&
            !root.TryGetProperty("EncryptedKeys", out keysProp))
            return new EncryptedContent
            {
                Content = content,
                Iv = iv,
                EncryptedKeys = encryptedKeys
            };
        foreach (var prop in keysProp.EnumerateObject())
        {
            if (Guid.TryParse(prop.Name, out var guid))
            {
                encryptedKeys[guid] = prop.Value.GetString() ?? "";
            }
        }

        return new EncryptedContent
        {
            Content = content,
            Iv = iv,
            EncryptedKeys = encryptedKeys
        };
    }

    public override void Write(Utf8JsonWriter writer, MessageContent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        // Write type discriminator
        var type = value switch
        {
            PlainTextContent => "plain",
            LinkContent => "link",
            ImageContent => "image",
            EncryptedContent => "encrypted",
            _ => "plain"
        };
        writer.WriteString("type", type);
        writer.WriteString("content", value.Content);

        // Write additional properties for EncryptedContent
        if (value is EncryptedContent encrypted)
        {
            writer.WriteString("iv", encrypted.Iv);
            writer.WriteStartObject("encryptedKeys");
            foreach (var kvp in encrypted.EncryptedKeys)
            {
                writer.WriteString(kvp.Key.ToString(), kvp.Value);
            }
            writer.WriteEndObject();
        }
        
        writer.WriteEndObject();
    }
}