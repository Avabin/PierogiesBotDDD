namespace Shared.Guilds.Commands;

public record UnsubscribeChannelCommand(ulong ChannelId, ulong GuildId) : GuildCommandBase(GuildId);