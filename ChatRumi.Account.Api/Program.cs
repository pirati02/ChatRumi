using ChatRumi.Account.Application;
using ChatRumi.Account.Application.Commands;
using ChatRumi.Account.Application.IntegrationEvents;
using ChatRumi.Account.Application.Projections;
using ChatRumi.Account.Application.Queries;
using FluentValidation;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
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
    x.AddConsumer<VerifyAccount.EventHandler>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("amqps://b-4560229a-1bc8-4813-92bb-bfb9441d9c53.mq.us-east-1.amazonaws.com:5671", h =>
        {
            h.Username("rabbit-admin");
            h.Password("rabbit-admin-pass");
        });

        cfg.ReceiveEndpoint("verify-account-event-queue",
            e => { e.ConfigureConsumer<VerifyAccount.EventHandler>(context); });
    });
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