using System.Collections.Immutable;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.SeedWork;

namespace Guilds.Domain.Aggregates.GuildAggregate;

public record GuildState : Entity
{
    public GuildState(string                            name,
                      ulong                             snowflakeId,
                      ImmutableList<SubscribedChannel>? subscribedChannels = null,
                      ImmutableList<IDelivery<IEvent>>? domainEvents       = null,
                      string                           id                 = "") : base(domainEvents ?? ImmutableList<IDelivery<IEvent>>.Empty, id)
    {
        Name               = name;
        SnowflakeId        = snowflakeId;
        SubscribedChannels = subscribedChannels ?? ImmutableList<SubscribedChannel>.Empty;
    }

    public static GuildState Empty =>
        new("", 0, ImmutableList<SubscribedChannel>.Empty, ImmutableList<IDelivery<IEvent>>.Empty);

    public string                            Name               { get; init; }
    public ulong                             SnowflakeId        { get; init; }
    public ImmutableList<SubscribedChannel> SubscribedChannels { get; init; }

    public void Deconstruct(out string name, out ulong snowflakeId, out ImmutableList<SubscribedChannel> subscribedChannels, out ImmutableList<IDelivery<IEvent>> domainEvents, out string? id)
    {
        name               = Name;
        snowflakeId        = SnowflakeId;
        subscribedChannels = SubscribedChannels;
        domainEvents       = DomainEvents;
        id                 = Id;
    }
}