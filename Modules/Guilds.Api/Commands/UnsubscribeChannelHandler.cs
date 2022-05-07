using Guilds.Domain.Aggregates.GuildAggregate;
using Shared.Core.Commands;
using Shared.Guilds.Commands;

namespace Guilds.Api.Commands;

public class UnsubscribeChannelHandler : CommandHandler<UnsubscribeChannelCommand>
{
    private readonly IGuildsAggregate _guildsAggregate;

    public UnsubscribeChannelHandler(IGuildsAggregate guildsAggregate)
    {
        _guildsAggregate = guildsAggregate;
    }

    protected override async Task HandleAsync(UnsubscribeChannelCommand command)
    {
        var guild = await _guildsAggregate.GetGuildAsync(command.GuildId);

        if (guild == null) return;

        await guild.UnsubscribeChannelAsync(command.ChannelId);
    }
}