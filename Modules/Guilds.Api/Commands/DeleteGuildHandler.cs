using Guilds.Domain.Aggregates.GuildAggregate;
using Shared.Core.Commands;
using Shared.Guilds.Commands;

namespace Guilds.Api.Commands;

public class DeleteGuildHandler : CommandHandler<DeleteGuildCommand>
{
    private readonly IGuildsAggregate _guildsAggregate;

    public DeleteGuildHandler(IGuildsAggregate guildsAggregate)
    {
        _guildsAggregate = guildsAggregate;
    }
    public override async Task HandleAsync(DeleteGuildCommand command)
    {
        await _guildsAggregate.DeleteAsync(command.GuildId);
    }
}