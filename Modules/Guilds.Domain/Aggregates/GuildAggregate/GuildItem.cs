using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shared.Core.Events;
using Shared.Core.MessageBroker;

namespace Guilds.Domain.Aggregates.GuildAggregate;

public record GuildItem : IGuildItem
{
    private readonly   IGuildService           _guildService;
    protected readonly ISubject<GuildState>    State = new BehaviorSubject<GuildState>(GuildState.Empty);
    public             IObservable<GuildState> StateObservable => State.AsObservable();

    public GuildItem(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<bool> HasStateAsync() => (await State.Select(x => x.Id is not "").FirstAsync());

    public async Task LoadOrCreateStateAsync(string id)
    {
        if (await HasStateAsync()) return;
        var newState = await _guildService.LoadOrCreateState(id);
        State.OnNext(newState);
    }

    public async Task LoadOrCreateStateAsync(ulong snowflakeId)
    {
        if (await HasStateAsync()) return;
        var newState = await _guildService.LoadOrCreateState(snowflakeId);
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

        var newState = await _guildService.AddDomainEventAsync(delivery, StateObservable);
        State.OnNext(newState);
    }

    public async Task RemoveDomainEventAsync(IDelivery<IEvent> delivery)
    {
        if (!await HasStateAsync()) return;

        var newState = await _guildService.RemoveDomainEventAsync(delivery, StateObservable);
        State.OnNext(newState);
    }

    public async Task DeleteStateAsync()
    {
        if (!await HasStateAsync()) return;
        
        var newState = await _guildService.DeleteStateAsync(StateObservable);
        State.OnNext(newState);
    }
}