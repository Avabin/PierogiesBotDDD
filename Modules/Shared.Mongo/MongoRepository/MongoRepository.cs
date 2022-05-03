using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.Persistence;
using Shared.Core.SeedWork;

[assembly: InternalsVisibleTo("Shared.Mongo.Tests")]

namespace Shared.Mongo.MongoRepository;

internal class MongoRepository<T> : IRepository<T> where T : Entity
{
    public           string       CollectionName { get; }
    private readonly IMongoClient _client;

    private readonly IOptions<MongoSettings>     _options;
    private readonly ILogger<MongoRepository<T>> _logger;
    protected        MongoSettings               Settings => _options.Value;

    private readonly Lazy<IMongoDatabase> _database;
    protected        IMongoDatabase       Database => _database.Value;

    private readonly Lazy<IMongoCollection<T>> _collection;
    protected        IMongoCollection<T>       Collection => _collection.Value;

    public MongoRepository(IMongoClient                client, IOptions<MongoSettings> options, string collectionName,
                           ILogger<MongoRepository<T>> logger)
    {
        CollectionName = collectionName;
        _client        = client;
        _options       = options;
        _logger        = logger;

        _database = new Lazy<IMongoDatabase>(() =>
        {
            _logger.LogTrace("Connecting to database {DatabaseName}", Settings.Database);
            return _client.GetDatabase(Settings.Database);
        });

        _collection = new Lazy<IMongoCollection<T>>(() =>
        {
            _logger.LogTrace("Connecting to collection {CollectionName}", CollectionName);
            return Database.GetCollection<T>(collectionName);
        });
    }

    public async Task<T?> FindByIdAsync(string id)
    {
        _logger.LogTrace("Finding entity {EntityType} by id {Id}", typeof(T).Name, id);
        return await Collection.Find(x => x.Id == id).SingleOrDefaultAsync();
    }

    public async Task<T?> FindOneByFieldAsync<TField>(Expression<Func<T, TField>> field, TField value)
    {
        _logger.LogTrace("Finding entity {EntityType} by field {Field} with value {Value}", typeof(T).Name, field,
                         value);
        var filter = Builders<T>.Filter.Eq(field, value);
        return await Collection.Find(filter).SingleOrDefaultAsync();
    }

    public async Task<T> InsertAsync(T entity)
    {
        _logger.LogTrace("Inserting entity {EntityType} with id {Id}", typeof(T).Name, entity.Id);
        await Collection.InsertOneAsync(entity);
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        _logger.LogTrace("Updating entity {EntityType} with id {Id}", typeof(T).Name, entity.Id);
        await Collection.FindOneAndReplaceAsync(x => x.Id == entity.Id, entity);
    }

    public async Task DeleteAsync(string id)
    {
        _logger.LogTrace("Deleting entity {EntityType} with id {Id}", typeof(T).Name, id);
        await Collection.FindOneAndDeleteAsync(x => x.Id == id);
    }

    public async Task AddDomainEventAsync(IDelivery<IEvent> @event, string id)
    {
        _logger.LogTrace("Adding domain event {EventType} to entity {EntityType} with id {Id}", @event.GetType(),
                         typeof(T).Name, id);
        var entity = await FindByIdAsync(id);
        if (entity is null) return;

        var domainEvents = entity.DomainEvents.ToList();
        domainEvents.Add(@event);

        var newEntity = entity with { DomainEvents = domainEvents.ToImmutableList() };
        await UpdateAsync(newEntity);
    }

    public async Task RemoveDomainEventAsync(IDelivery<IEvent> @event, string id)
    {
        _logger.LogTrace("Removing domain event {EventType} from entity {EntityType} with id {Id}", @event.GetType(),
                         typeof(T).Name, id);
        var entity = await FindByIdAsync(id);
        if (entity is null) return;

        var domainEvents = entity.DomainEvents.ToList();
        domainEvents.Remove(@event);

        var newEntity = entity with { DomainEvents = domainEvents.ToImmutableList() };
        await UpdateAsync(newEntity);
    }
}