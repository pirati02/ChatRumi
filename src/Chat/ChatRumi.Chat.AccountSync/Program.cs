using ChatRum.InterCommunication;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Chat.AccountSync;
using ChatRumi.Chat.Application;
using ChatRumi.Chat.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<KafkaOptions>().BindConfiguration(KafkaOptions.Name);
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddHostedService();
builder.Services.AddHostedService<AccountModifiedConsumerBackgroundService>();
builder.Services.AddOpenTelemetryObservability(builder.Configuration);

var app = builder.Build();

app.Run();