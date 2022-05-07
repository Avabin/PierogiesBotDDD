using Shared.Core.Commands;

namespace Shared.Guilds.Commands;

public record ChangeGuildNameCommand(string Name, ulong GuildId) : Command;