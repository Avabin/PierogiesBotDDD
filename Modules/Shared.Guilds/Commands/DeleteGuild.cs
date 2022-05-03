namespace Shared.Guilds.Commands;

public record DeleteGuild(ulong GuildId) : GuildCommandBase(GuildId)
{
}