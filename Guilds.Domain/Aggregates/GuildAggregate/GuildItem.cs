using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.SeedWork;

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

public record GuildItem : IGuildItem
{
    private readonly   IGuildService           _guildService;
    protected readonly ISubject<GuildState>    State = new BehaviorSubject<GuildState>(GuildState.Empty);
    public             IObservable<GuildState> StateObservable => State.AsObservable();

    public GuildItem(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<bool> HasStateAsync() => (await State.Select(x => x != GuildState.Empty).FirstAsync());

    public async Task LoadStateAsync(string id)
    {
        if (await HasStateAsync()) return;
        var newState = await _guildService.LoadStateAsync(id);
        State.OnNext(newState);
    }
    public async Task LoadStateAsync(ulong snowflakeId)
    {
        if (await HasStateAsync()) return;
        var newState = await _guildService.LoadStateAsync(snowflakeId);
        State.OnNext(newState);
    }
    
    public async Task ChangeNameAsync(string name)
    {
        var newState = await _guildService.ChangeNameAsync(name, StateObservable);
        State.OnNext(newState);
    }
    
    // Subscribe to channel
    public async Task SubscribeChannelAsync(string name, ulong channelId)
    {
        var newState = await _guildService.SubscribeChannelAsync(name, channelId, StateObservable);
        State.OnNext(newState);
    }
    
    // Unsubscribe from channel
    public async Task UnsubscribeChannelAsync(ulong channelId)
    {
        var newState = await _guildService.UnsubscribeChannelAsync(channelId, StateObservable);
        State.OnNext(newState);
    }

    public async Task AddDomainEventAsync(IDelivery<IEvent> delivery)
    {
        if (!await HasStateAsync()) return;
        
        await _guildService.AddDomainEventAsync(delivery, StateObservable);
    }

    public async Task RemoveDomainEventAsync(IDelivery<IEvent> delivery)
    {
        if (!await HasStateAsync()) return;
        
        await _guildService.RemoveDomainEventAsync(delivery, StateObservable);
    }
}