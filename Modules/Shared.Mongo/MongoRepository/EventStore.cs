using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.Core.Events;
using Shared.Core.MessageBroker;

namespace Shared.Mongo.MongoRepository;

internal class EventStore : IEventStore
{
    private const    string                  CollectionName = "Events";
    private readonly IOptions<MongoSettings> _options;
    protected        MongoSettings           Settings => _options.Value;

    private readonly Lazy<IMongoCollection<IDelivery<IEvent>>> _events;
    protected        IMongoCollection<IDelivery<IEvent>>       Events => _events.Value;

    public EventStore(IMongoClient client, IOptions<MongoSettings> options)
    {
        _options = options;

        _events = new Lazy<IMongoCollection<IDelivery<IEvent>>>(() => client.GetDatabase(Settings.Database).GetCollection<IDelivery<IEvent>>(CollectionName));
    }
    public IQueryable<IDelivery<IEvent>> Query() => Events.AsQueryable();

    public async Task AddAsync(IDelivery<IEvent> delivery) => 
        await Events.InsertOneAsync(delivery);
}