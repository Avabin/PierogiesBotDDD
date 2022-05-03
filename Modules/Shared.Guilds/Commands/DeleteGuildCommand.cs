namespace Shared.Guilds.Commands;

public record DeleteGuildCommand(ulong GuildId) : GuildCommandBase(GuildId)
{
}