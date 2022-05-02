using Guilds.Domain.Aggregates.GuildAggregate;
using Shared.Core.Commands;
using Shared.Guilds.Commands;

namespace Guilds.Api.Commands;

public class ChangeGuildNameHandler : CommandHandler<ChangeGuildName>
{
    private readonly IGuildsAggregate _guildsAggregate;

    public ChangeGuildNameHandler(IGuildsAggregate guildsAggregate)
    {
        _guildsAggregate = guildsAggregate;
    }
    public override async Task HandleAsync(ChangeGuildName command)
    {
        var guild = await _guildsAggregate.GetGuildAsync(command.GuildId);

        await guild.ChangeNameAsync(command.Name);
    }
}