using Shared.Core.Commands;

namespace Shared.Guilds.Commands;

public record DeleteGuildCommand(ulong GuildId) : Command
{
}