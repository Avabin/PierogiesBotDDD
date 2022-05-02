using Guilds.Domain.Aggregates.GuildAggregate;
using Shared.Core.Commands;
using Shared.Guilds.Commands;

namespace Guilds.Api.Commands;

public class CreateGuildHandler : CommandHandler<CreateGuild>
{
    private readonly IGuildsAggregate _guildsAggregate;

    public CreateGuildHandler(IGuildsAggregate guildsAggregate)
    {
        _guildsAggregate    = guildsAggregate;
    }
    public override async Task HandleAsync(CreateGuild command)
    {
        var guild = await _guildsAggregate.GetGuildAsync(command.SnowflakeId);
        if (guild == null)
        {
            guild = await _guildsAggregate.CreateGuildAsync(command.SnowflakeId);
            await guild.ChangeNameAsync(command.Name);
            if (Context != null) await guild.AddDomainEventAsync(Context);
        }
    }
}