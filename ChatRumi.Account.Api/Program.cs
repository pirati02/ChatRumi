using ChatRumi.Account.Application;
using ChatRumi.Account.Application.Commands;
using ChatRumi.Account.Application.Options;
using ChatRumi.Account.Application.Projections;
using ChatRumi.Account.Application.Queries;
using ChatRumi.Account.Application.Services;
using FluentValidation;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Weasel.Core;
using IMediator = MediatR.IMediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(config => config.RegisterServicesFromAssemblies(Application.Assembly));
builder.Services.AddValidatorsFromAssembly(Application.Assembly);

builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("Marten")!);
    options.UseSystemTextJsonForSerialization();
    if (builder.Environment.IsDevelopment())
    {
        options.AutoCreateSchemaObjects = AutoCreate.All;
    }

    options.Projections.Add<AccountProjectionTransform>(ProjectionLifecycle.Inline);
    options.Schema.For<AccountProjection>()
        .UniqueIndex(x => x.UserName)
        .UniqueIndex(x => x.Email);

    options.Events.StreamIdentity = StreamIdentity.AsGuid;
}).AddAsyncDaemon(DaemonMode.Solo);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ChatRumi.Account.Application.IntegrationEvents.VerifyAccount.EventHandler>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("MassTransit"), h =>
        {
            h.Username("admin");
            h.Password("rbadminpass");
        });

        cfg.ReceiveEndpoint("verify-account-event-queue",
            e =>
            {
                e.ConfigureConsumer<ChatRumi.Account.Application.IntegrationEvents.VerifyAccount.EventHandler>(context);
            });
    });
});
builder.Services.AddScoped<IConnectionMultiplexer>(sp =>
{
    var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(new ConfigurationOptions
    {
        EndPoints = { { options.Host, options.Port } },
        User = options.User,
        Password = options.Password
    });
});
builder.Services.Configure<SmsOfficeOptions>(builder.Configuration.GetSection(SmsOfficeOptions.Name));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.Name));
builder.Services.AddScoped<ISmsService, SmsOfficeService>();
builder.Services.AddHttpClient<ISmsService, SmsOfficeService>((sp, httpClient) =>
{
    var options = sp.GetRequiredService<IOptions<SmsOfficeOptions>>().Value;
    httpClient.BaseAddress = new Uri(options.BaseUrl);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("", async ([FromBody] CreateAccount.Command command, IMediator mediator) =>
    {
        var result = await mediator.Send(command);
        return result.Match(
            value => Results.Created(value.ToString(), value.ToString()),
            Results.BadRequest
        );
    })
    .WithName("create-account")
    .WithOpenApi();

app.MapPatch("{accountId:guid}", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new VerifyAccount.Command(accountId));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("verify-account")
    .WithOpenApi();


app.MapGet("{accountId:guid}", async ([FromRoute] Guid accountId, IMediator mediator) =>
    {
        var result = await mediator.Send(new GetAccount.Query(accountId));
        return result.Match(
            Results.Ok,
            Results.NotFound
        );
    })
    .WithName("get-account")
    .WithOpenApi();

app.Run();