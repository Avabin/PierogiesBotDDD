using System.Collections.Immutable;
using Shared.Core.Events;
using Shared.Guilds.Views;

namespace Shared.Guilds.Queries;

public record QuerySubscribedChannelsResult(ImmutableList<SubscribedChannelView> Channels) : Event
{
    public static QuerySubscribedChannelsResult Of(IEnumerable<SubscribedChannelView> channels) => new(channels.ToImmutableList());
    public static QuerySubscribedChannelsResult Empty => new(ImmutableList<SubscribedChannelView>.Empty);
}