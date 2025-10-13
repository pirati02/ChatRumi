using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application;

public static class DefaultJsonContentOptions
{
    public static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

        options.TypeInfoResolverChain.Insert(0,
            new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    ti =>
                    {
                        if (ti.Type != typeof(MessageContent)) return;
                        ti.PolymorphismOptions = new JsonPolymorphismOptions
                        {
                            TypeDiscriminatorPropertyName = "type",
                            IgnoreUnrecognizedTypeDiscriminators = false
                        };
                        ti.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(typeof(PlainTextContent), "plain"));
                        ti.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(typeof(LinkContent), "link"));
                        ti.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(typeof(ImageContent), "image"));
                    }
                }
            });

        return options;
    }    
}