namespace Shared.Guilds.Commands;

public record SubscribeChannelCommand(string Name, ulong ChannelId, ulong GuildId) : GuildCommandBase(GuildId);