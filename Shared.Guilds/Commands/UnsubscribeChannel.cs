using Shared.Core.Commands;

namespace Shared.Guilds.Commands;

public record UnsubscribeChannel(ulong ChannelId, ulong GuildId) : GuildCommandBase(GuildId);