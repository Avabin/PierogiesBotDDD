using Shared.Core.Events;
using Shared.Core.MessageBroker;

namespace Guilds.Domain.Aggregates.GuildAggregate;

public interface IGuildItem
{
    IObservable<GuildState> StateObservable { get; }
    Task<bool>              HasStateAsync();
    Task                    LoadStateAsync(string         id);
    Task                    LoadStateAsync(ulong          snowflakeId);
    Task                    ChangeNameAsync(string        name);
    Task                    SubscribeChannelAsync(string  name, ulong channelId);
    Task                    UnsubscribeChannelAsync(ulong channelId);

    Task AddDomainEventAsync(IDelivery<IEvent>    delivery);
    Task RemoveDomainEventAsync(IDelivery<IEvent> delivery);
}