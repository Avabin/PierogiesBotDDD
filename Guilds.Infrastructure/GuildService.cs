using System.Collections.Immutable;
using System.Reactive.Linq;
using Guilds.Domain.Aggregates.GuildAggregate;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.Notifications;
using Shared.Core.Persistence;
using Shared.Guilds.Notifications;
using Shared.Mongo.MongoRepository;

namespace Guilds.Infrastructure;

public class GuildService : IGuildService
{
    private readonly IRepository<GuildState> _repository;
    private readonly IMessageBroker          _messageBroker;

    public GuildService(IMongoRepositoryFactory repositoryFactory, IMessageBroker messageBroker)
    {
        _repository    = repositoryFactory.Create<GuildState>("Guilds");
        _messageBroker = messageBroker;
    }

    public async Task<GuildState> LoadStateAsync(string id)
    {
        var existing = await _repository.FindByIdAsync(id);
        if (existing != null) return existing;

        var newState = await _repository.InsertAsync(GuildState.Empty);
        return newState;
    }

    public async Task<GuildState> LoadStateAsync(ulong snowflakeId)
    {
        var existing = await _repository.FindOneByFieldAsync(x => x.SnowflakeId, snowflakeId);
        if (existing != null) return existing;

        var newState = await _repository.InsertAsync(GuildState.Empty with { SnowflakeId = snowflakeId });
        return newState;
    }

    public async Task<GuildState> ChangeNameAsync(string name, IObservable<GuildState> state)
    {
        var newState = await Apply(old => old with { Name = name }, state);
        await _messageBroker.NotifyAsync(new GuildNameChanged(name), newState.SnowflakeId.ToString());
        return newState;
    }

    public async Task<GuildState> SubscribeChannelAsync(string name, ulong channelId, IObservable<GuildState> state)
    {
        var newState = await Apply(old => old with { SubscribedChannels = old.SubscribedChannels.Add(new SubscribedChannel(name, channelId)) }, state);
        await _messageBroker.NotifyAsync(new SubscribedToChannel(name, channelId), newState.SnowflakeId.ToString());
        
        return newState;
    }

    public async Task<GuildState> UnsubscribeChannelAsync(ulong channelId, IObservable<GuildState> state)
    {
        var newState = await Apply(old => old with { SubscribedChannels = old.SubscribedChannels.RemoveAll(x => x.ChannelId == channelId)}, state);
        await _messageBroker.NotifyAsync(new UnsubscribedFromChannel(channelId), newState.SnowflakeId.ToString());
        
        return newState;
    }

    public async Task AddDomainEventAsync(IDelivery<IEvent> delivery, IObservable<GuildState> stateObservable) => 
        await Apply(old => old with {DomainEvents = old.DomainEvents.Add(delivery)}, stateObservable);

    public async Task RemoveDomainEventAsync(IDelivery<IEvent> @event, IObservable<GuildState> stateObservable) => 
        await Apply(old => old with {DomainEvents = old.DomainEvents.Remove(@event)}, stateObservable);

    private async Task<GuildState> Apply(Func<GuildState, GuildState> transform, IObservable<GuildState> state)
    {
        var currentState = await state.FirstAsync();

        var newState = transform(currentState);
        await _repository.UpdateAsync(newState);
        return newState;
    }
}