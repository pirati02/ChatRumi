using ChatRum.InterCommunication;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Feed.AccountSync;
using ChatRumi.Feed.Application;
using ChatRumi.Feed.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<KafkaOptions>().BindConfiguration(KafkaOptions.Name);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHostedService<AccountModifiedConsumerBackgroundService>();
builder.Services.AddOpenTelemetryObservability(builder.Configuration);

var app = builder.Build();

app.Run();