using Shared.Core.Events;
using Shared.Core.MessageBroker;

namespace Guilds.Domain.Aggregates.GuildAggregate;

public interface IGuildService
{
    Task<GuildState> LoadOrCreateState(string                    id);
    Task<GuildState> LoadOrCreateState(ulong                     snowflakeId);
    Task<GuildState> ChangeNameAsync(string                   name,      IObservable<GuildState> state);
    Task<GuildState> SubscribeChannelAsync(string             name,      ulong                   channelId, IObservable<GuildState> state);
    Task<GuildState> UnsubscribeChannelAsync(ulong            channelId, IObservable<GuildState> state);
    Task<GuildState> AddDomainEventAsync(IDelivery<IEvent>    delivery,  IObservable<GuildState> stateObservable);
    Task<GuildState> RemoveDomainEventAsync(IDelivery<IEvent> @event,    IObservable<GuildState> stateObservable);
    Task<GuildState> DeleteStateAsync(IObservable<GuildState> stateObservable);
    Task<bool>           ExistsAsync(ulong                        snowflakeId);
}