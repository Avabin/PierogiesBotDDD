namespace Shared.Guilds.Commands;

public record SubscribeChannel(string Name, ulong ChannelId, ulong GuildId) : GuildCommandBase(GuildId);