using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Account.Application.Events;
using ChatRumi.Infrastructure;
using MassTransit;

namespace ChatRumi.Account.Api;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddPresentation(IConfiguration configuration)
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
            var useInMemoryMassTransit = configuration.GetValue("IntegrationTesting:UseInMemoryMassTransit", false)
                                         || string.Equals(
                                             configuration["ASPNETCORE_ENVIRONMENT"],
                                             "Integration",
                                             StringComparison.OrdinalIgnoreCase);

            services.AddMassTransit(x =>
            {
                x.AddConsumer<VerifyAccount.Handler>();

                if (useInMemoryMassTransit)
                {
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ReceiveEndpoint("verify-account-event-queue",
                            e => { e.ConfigureConsumer<VerifyAccount.Handler>(context); });
                    });
                }
                else
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        // Aspire injects a full AMQP URL; MassTransit Host(string) expects a hostname only.
                        var massTransitUrl = configuration["MassTransit_Url"]
                                             ?? configuration.GetConnectionString("MassTransit")
                                             ?? throw new InvalidOperationException(
                                                 "MassTransit is not configured (MassTransit_Url or ConnectionStrings:MassTransit).");
                        if (!massTransitUrl.Contains("://", StringComparison.Ordinal))
                            massTransitUrl = $"amqp://{massTransitUrl.TrimEnd('/')}:5672/";

                        if (!Uri.TryCreate(massTransitUrl, UriKind.Absolute, out var amqpUri))
                            throw new InvalidOperationException($"MassTransit URL is not a valid URI: {massTransitUrl}");

                        var massTransitUser = configuration["MassTransit_User"];
                        var massTransitPassword = configuration["MassTransit_Password"];
                        if (string.IsNullOrEmpty(massTransitUser) && !string.IsNullOrEmpty(amqpUri.UserInfo))
                        {
                            var parts = amqpUri.UserInfo.Split(':', 2);
                            massTransitUser = Uri.UnescapeDataString(parts[0]);
                            massTransitPassword = parts.Length > 1
                                ? Uri.UnescapeDataString(parts[1])
                                : "";
                        }

                        if (string.IsNullOrEmpty(massTransitUser))
                            throw new InvalidOperationException("MassTransit_User is not configured.");
                        if (massTransitPassword is null)
                            throw new InvalidOperationException("MassTransit_Password is not configured.");

                        var hostBuilder = new UriBuilder(amqpUri)
                        {
                            Scheme = "rabbitmq",
                            UserName = "",
                            Password = ""
                        };
                        cfg.Host(hostBuilder.Uri, h =>
                        {
                            h.Username(massTransitUser);
                            h.Password(massTransitPassword);
                        });

                        cfg.ReceiveEndpoint("verify-account-event-queue",
                            e =>
                            {
                                e.ConfigureConsumer<VerifyAccount.Handler>(
                                    context);
                            });
                    });
                }
            });
            services.AddChatRumiJwtAuthentication(configuration);

            services.AddConsulService(configuration);
            services.AddOpenTelemetryObservability(configuration);
        }
    }
}