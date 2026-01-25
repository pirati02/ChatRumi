using ChatRum.InterCommunication;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Friendship.AccountSync;
using ChatRumi.Friendship.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<KafkaOptions>().BindConfiguration(KafkaOptions.Name);
builder.Services.AddApplication();
builder.Services.AddHostedService<AccountCreatedConsumerBackgroundService>();
// builder.Services.AddHostedService<AccountModifiedConsumerBackgroundService>();
builder.Services.AddOpenTelemetryObservability(builder.Configuration);

var app = builder.Build();
app.Run();