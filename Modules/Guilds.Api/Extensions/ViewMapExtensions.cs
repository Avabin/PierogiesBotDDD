using System.Collections.Immutable;
using Guilds.Domain.Aggregates.GuildAggregate;
using Shared.Guilds.Views;

namespace Guilds.Api.Extensions;

public static class ViewMapExtensions
{
    public static GuildView ToView(this GuildState guild)
    {
        return new GuildView(guild.Name, guild.SnowflakeId,
                             guild.SubscribedChannels.Select(ToView).ToImmutableList(), guild.Id);
    }

    public static SubscribedChannelView ToView(this SubscribedChannel channel) => new(channel.Name, channel.ChannelId);
}