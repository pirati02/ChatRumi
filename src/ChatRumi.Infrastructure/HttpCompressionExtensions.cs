namespace ChatRumi.Infrastructure;

public static class HttpCompressionExtensions
{
    public static IServiceCollection AddChatRumiResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        return services;
    }
}
