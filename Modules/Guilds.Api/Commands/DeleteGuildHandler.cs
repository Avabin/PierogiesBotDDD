using Guilds.Domain.Aggregates.GuildAggregate;
using Shared.Core.Commands;
using Shared.Guilds.Commands;

namespace Guilds.Api.Commands;

public class DeleteGuildHandler : CommandHandler<DeleteGuild>
{
    private readonly IGuildsAggregate _guildsAggregate;

    public DeleteGuildHandler(IGuildsAggregate guildsAggregate)
    {
        _guildsAggregate = guildsAggregate;
    }
    public override async Task HandleAsync(DeleteGuild command)
    {
        await _guildsAggregate.DeleteAsync(command.GuildId);
    }
}