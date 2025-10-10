namespace ChatRumi.Feed.Api;

public static class Dependency
{
    public static void AddApi(
        this IServiceCollection services
    )
    {
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policyBuilder =>
            {
                policyBuilder.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
    }
}