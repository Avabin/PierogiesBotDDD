using System.Collections.Immutable;

namespace Shared.Guilds.Views;

public record GuildView(string Name, ulong SnowflakeId, ImmutableList<SubscribedChannelView> SubscribedChannels,
                        string Id)
{
}