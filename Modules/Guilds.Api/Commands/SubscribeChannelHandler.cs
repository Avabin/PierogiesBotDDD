using System.Reactive.Linq;
using Guilds.Domain.Aggregates.GuildAggregate;
using Shared.Core.Commands;
using Shared.Guilds.Commands;

namespace Guilds.Api.Commands;

public class SubscribeChannelHandler : CommandHandler<SubscribeChannel>
{
    private readonly IGuildsAggregate _guildsAggregate;

    public SubscribeChannelHandler(IGuildsAggregate guildsAggregate)
    {
        _guildsAggregate = guildsAggregate;
    }

    public override async Task HandleAsync(SubscribeChannel command)
    {
        var guild = await _guildsAggregate.GetGuildAsync(command.GuildId);

        if (guild is not null)
        {
            var channels = await guild.StateObservable.Select(x => x.SubscribedChannels).FirstAsync();
            if (channels.Any(x => x.ChannelId == command.ChannelId)) return;
            await guild.SubscribeChannelAsync(command.Name, command.ChannelId);
            if (Context is not null) await guild.AddDomainEventAsync(Context);
        }
    }
}