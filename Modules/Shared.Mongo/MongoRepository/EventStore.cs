using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.Core.MessageBroker;

namespace Shared.Mongo.MongoRepository;

internal class EventStore : IEventStore
{
    internal const    string                  CollectionName = "Events";
    private readonly IOptions<MongoSettings> _options;
    private readonly ILogger<EventStore>     _logger;
    protected        MongoSettings           Settings => _options.Value;

    private readonly Lazy<IMongoCollection<Delivery>> _events;
    protected        IMongoCollection<Delivery>       Events => _events.Value;

    public EventStore(IMongoClient client, IOptions<MongoSettings> options, ILogger<EventStore> logger)
    {
        _options = options;
        _logger  = logger;

        _events = new Lazy<IMongoCollection<Delivery>>(() =>
        {
            _logger.LogDebug("Getting events collection");
            _logger.LogTrace("Collection name: {CollectionName}", CollectionName);
            return client.GetDatabase(Settings.Database).GetCollection<Delivery>(CollectionName);
        });
    }

    public IQueryable<Delivery> Query()
    {
        _logger.LogTrace("Events query requested");
        return Events.AsQueryable();
    }

    public async Task AddAsync(Delivery delivery)
    {
        _logger.LogDebug("Adding event {EventType}", delivery.Data.GetType().Name);
        _logger.LogTrace("Adding event {@Event}", delivery);
        await Events.InsertOneAsync(delivery);
    }
}