using Shared.Core.Commands;

namespace Shared.Guilds.Commands;

public record CreateGuild(string Name, ulong SnowflakeId) : Command
{
}