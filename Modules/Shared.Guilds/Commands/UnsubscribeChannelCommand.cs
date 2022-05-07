using Shared.Core.Commands;

namespace Shared.Guilds.Commands;

public record UnsubscribeChannelCommand(ulong ChannelId, ulong GuildId) : Command;