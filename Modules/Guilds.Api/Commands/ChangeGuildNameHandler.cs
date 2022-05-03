using System.Reactive.Linq;
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

        if (guild is not null)
        {
            var currentGuildName = await guild.StateObservable.Select(x => x.Name).FirstAsync();
            if (currentGuildName.Equals(command.Name)) return;
            await guild.ChangeNameAsync(command.Name);
            if (Context != null) await guild.AddDomainEventAsync(Context);
        }
    }
}