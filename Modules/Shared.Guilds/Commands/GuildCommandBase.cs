using Shared.Core.Commands;

namespace Shared.Guilds.Commands;

public abstract record GuildCommandBase(ulong GuildId) : Command;