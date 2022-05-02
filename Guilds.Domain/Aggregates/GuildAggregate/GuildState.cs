using System.Collections.Immutable;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.SeedWork;

namespace Guilds.Domain.Aggregates.GuildAggregate;

public record GuildState(
    string Name, 
    ulong SnowflakeId,
    ImmutableList<SubscribedChannel> SubscribedChannels,
    ImmutableList<IDelivery<IEvent>>DomainEvents,
    string? Id = "") : Entity(DomainEvents, Id)
{
    public static GuildState Empty => new( "", 0, ImmutableList<SubscribedChannel>.Empty, ImmutableList<IDelivery<IEvent>>.Empty);
}