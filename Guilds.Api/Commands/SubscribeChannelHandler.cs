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
        
        await guild.SubscribeChannelAsync(command.Name, command.ChannelId);
    }
}