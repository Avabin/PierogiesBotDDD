using System.Reactive.Linq;
using Guilds.Infrastructure.RabbitMq;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.MessageBroker;
using Shared.Guilds.Commands;
using Shared.Guilds.Notifications;
using Shared.Guilds.Queries;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions().Configure<RabbitMqSettings>(builder.Configuration.GetRequiredSection("RabbitMQ")).AddSingleton<IMessageBroker, RabbitMqMessageBroker>();
var app     = builder.Build();

app.MapGet("/", async ([FromServices] IMessageBroker broker) =>
{
    var guild = await broker.SendAndReceiveAsync<QueryGuildResult, QueryGuild>(new QueryGuild(123123123ul));
    return Results.Ok(guild);
});

app.MapPost("/{id:long}",
            async ([FromQuery] string name, [FromRoute] ulong id, [FromServices] IMessageBroker broker) =>
            {
                var notificationsObservable = broker.GetNotificationsObservable<GuildNameChanged>(id.ToString());
                await broker.SendToQueueAsync(new ChangeGuildName(name, id), IMessageBroker.RpcQueueName);
                await notificationsObservable.FirstAsync().Timeout(TimeSpan.FromSeconds(15));
                
                return Results.Ok();
            });

app.Run();