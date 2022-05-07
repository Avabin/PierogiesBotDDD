using Shared.Core.Commands;

namespace Shared.Guilds.Commands;

public record SubscribeChannelCommand(string Name, ulong ChannelId, ulong GuildId) : Command;