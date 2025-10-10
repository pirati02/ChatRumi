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

        if (!root.TryGetProperty("type", out var typeProp))
            return new PlainTextContent { Content = root.GetProperty("content").GetString()! };
        var type = typeProp.GetString();
        return type switch
        {
            "plain" => root.Deserialize<PlainTextContent>(options)!,
            "link"  => root.Deserialize<LinkContent>(options)!,
            "image" => root.Deserialize<ImageContent>(options)!,
            _       => throw new JsonException($"Unknown type {type}")
        };

        // fallback for legacy data
    }

    public override void Write(Utf8JsonWriter writer, MessageContent value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, (object)value, options);
}