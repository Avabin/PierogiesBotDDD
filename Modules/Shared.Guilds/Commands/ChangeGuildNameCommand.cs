namespace Shared.Guilds.Commands;

public record ChangeGuildNameCommand(string Name, ulong GuildId) : GuildCommandBase(GuildId);