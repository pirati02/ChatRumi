namespace ChatRumi.Feed.Api;

public static class Dependency
{
    public static void AddApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
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
        services.AddMediatR(config =>
            config.RegisterServicesFromAssemblies(Application.Assembly));
    }
}