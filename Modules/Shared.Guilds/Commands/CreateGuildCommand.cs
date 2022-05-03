using Shared.Core.Commands;

namespace Shared.Guilds.Commands;

public record CreateGuildCommand(string Name, ulong SnowflakeId) : Command
{
}