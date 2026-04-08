using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatRumi.Infrastructure;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddChatRumiJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(o =>
                    !string.IsNullOrWhiteSpace(o.Issuer)
                    && !string.IsNullOrWhiteSpace(o.Audience)
                    && !string.IsNullOrWhiteSpace(o.SigningKey)
                    && o.SigningKey.Length >= 32
                    && o.AccessTokenExpirationMinutes is >= 1 and <= 10080
                    && o.RefreshTokenExpirationDays is >= 1 and <= 365,
                "Jwt: Issuer, Audience, and SigningKey (min 32 chars) are required; AccessTokenExpirationMinutes must be 1–10080; RefreshTokenExpirationDays must be 1–365.")
            .ValidateOnStart();

        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    private sealed class JwtBearerPostConfigureOptions(IOptions<JwtOptions> jwtOptions) : IPostConfigureOptions<JwtBearerOptions>
    {
        public void PostConfigure(string? name, JwtBearerOptions options)
        {
            if (name != JwtBearerDefaults.AuthenticationScheme)
            {
                return;
            }

            var jwt = jwtOptions.Value;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessTokenValues = context.Request.Query["access_token"];
                    var accessToken = accessTokenValues.FirstOrDefault(v => !string.IsNullOrEmpty(v));
                    var path = context.HttpContext.Request.Path;
                    var hasBearerHeader = context.Request.Headers.Authorization.Any(h =>
                        !string.IsNullOrWhiteSpace(h) &&
                        h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase));

                    var isHubPath = path.StartsWithSegments("/hub/chat")
                        || path.StartsWithSegments("/hub/friendship")
                        || path.StartsWithSegments("/hub/notifications");
                    var isAttachmentPath = path.StartsWithSegments("/api/chat/attachments")
                        || path.StartsWithSegments("/chat/attachments")
                        || path.StartsWithSegments("/api/feed/attachments")
                        || path.StartsWithSegments("/feed/attachments");

                    if (!string.IsNullOrEmpty(accessToken) &&
                        (isHubPath || (isAttachmentPath && !hasBearerHeader)))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        }
    }
}
