using ChatRum.InterCommunication;
using ChatRum.InterCommunication.Telemetry;
using ChatRumi.Friendship.AccountSync;
using ChatRumi.Friendship.Application;
using ChatRumi.Friendship.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<KafkaOptions>().BindConfiguration(KafkaOptions.Name);
builder.Services.AddScoped<IFriendshipHubContextProxy, FriendshipHubContextProxy>();
builder.Services.AddApplication();
builder.Services.AddHostedService<AccountCreatedConsumerBackgroundService>();
// builder.Services.AddHostedService<AccountModifiedConsumerBackgroundService>();
builder.Services.AddOpenTelemetryObservability(builder.Configuration);

var app = builder.Build();
app.Run();