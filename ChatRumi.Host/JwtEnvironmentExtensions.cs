using Aspire.Hosting.ApplicationModel;

namespace ChatRumi.Host;

internal static class JwtEnvironmentExtensions
{
    internal static IResourceBuilder<ProjectResource> WithChatRumiJwt(
        this IResourceBuilder<ProjectResource> project,
        IResourceBuilder<ParameterResource> jwtSigningKey) =>
        project
            .WithEnvironment("Jwt__Issuer", "ChatRumi")
            .WithEnvironment("Jwt__Audience", "ChatRumi")
            .WithEnvironment("Jwt__SigningKey", jwtSigningKey);
}
