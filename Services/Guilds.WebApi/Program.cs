using System.Reactive.Linq;
using System.Text.Json;
using Guilds.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.MessageBroker;
using Shared.Guilds;
using Shared.Guilds.Commands;
using Shared.Guilds.Notifications;
using Shared.Guilds.Queries;
using Shared.Guilds.Views;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureAppConfiguration(c => c.AddEnvironmentVariables("PB_"));
builder.Host.AddSerilog("Guilds.WebApi");
builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();

app.MapGet("/{id:long}", async ([FromServices] IMessageBroker broker, [FromRoute] ulong id) =>
{
    var result = await broker.SendAndReceiveAsync<QueryGuild, QueryGuildResult>(new QueryGuild(id));
    return result.Guild.Id is not "" ? Results.Ok(result.Guild) : Results.NotFound();
});

app.MapPost("/{id:long}",
            async ([FromBody] JsonElement guildDto, [FromServices] IMessageBroker broker, [FromRoute] ulong id) =>
            {
                var name = guildDto.GetProperty("name").GetString();
                if (name is null)   return Results.BadRequest(new {property = "name", error ="name is required"});
                await broker.SendCommandAsync(new CreateGuildCommand(name, id));

                return Results.Ok(id);
            });

app.MapPost("/{id:long}/name",
            async ([FromQuery] string name, [FromRoute] ulong id, [FromServices] IMessageBroker broker) =>
            {
                var notificationsObservable = broker.GetNotificationsObservable<GuildNameChanged>(id.ToString());
                await broker.SendToQueueAsync(new ChangeGuildNameCommand(name, id), IMessageBroker.RpcQueueName);
                await notificationsObservable.FirstAsync();

                return Results.NoContent();
            });

app.MapPost("/{id:long}/channels",
            async ([FromBody]     SubscribedChannelView channel, [FromRoute] ulong id,
                   [FromServices] IMessageBroker        broker) =>
            {
                var notificationObservable = broker.GetNotificationsObservable<SubscribedToChannel>(id.ToString());
                await broker.SendToQueueAsync(channel.ToCommand(id), IMessageBroker.RpcQueueName);
                await notificationObservable.FirstAsync();

                return Results.NoContent();
            });

app.MapDelete("/{id:long}/channels/{channelId:long}",
              async ([FromRoute] ulong id, [FromRoute] ulong channelId, [FromServices] IMessageBroker broker) =>
              {
                  var notificationObservable =
                      broker.GetNotificationsObservable<UnsubscribedFromChannel>(id.ToString());
                  await broker.SendToQueueAsync(new UnsubscribeChannelCommand(channelId, id), IMessageBroker.RpcQueueName);
                  await notificationObservable.FirstAsync();

                  return Results.NoContent();
              });

app.Run();