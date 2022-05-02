namespace Shared.Guilds.Commands;

public record ChangeGuildName(string Name, ulong GuildId) : GuildCommandBase(GuildId);