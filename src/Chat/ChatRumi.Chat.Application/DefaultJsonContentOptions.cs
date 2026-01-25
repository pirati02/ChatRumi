using System.Text.Json;
using System.Text.Json.Serialization;
using ChatRumi.Chat.Application.Dto;

namespace ChatRumi.Chat.Application;

public static class DefaultJsonContentOptions
{
    public static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Converters = 
            { 
                new JsonStringEnumConverter(),
                new MessageContentConverter()
            }
        };

        return options;
    }    
}