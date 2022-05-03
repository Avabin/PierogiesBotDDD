using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Domain.Aggregates.GuildAggregate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using NUnit.Framework;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.SeedWork;
using Shared.Mongo.MongoRepository;
using Shared.Mongo.Serializers;

namespace Shared.Mongo.Tests;

[TestFixture]
[Category("Integration")]
public class MongoRepositoryIntegrationTests
{
    private IMongoClient                         _mongoClient;
    private IOptions<MongoSettings>              _options;
    private ILogger<MongoRepository<TestEntity>> _logger;

    private MongoRepository<TestEntity> Create(string collectionName) =>
        new(_mongoClient, _options, collectionName, _logger);

    [OneTimeSetUp]
    public static void OneTimeSetUp()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Event))) BsonClassMap.RegisterClassMap<Event>();
        if (!BsonClassMap.IsClassMapRegistered(typeof(TestEvent))) BsonClassMap.RegisterClassMap<TestEvent>();
        // Delivery
        if (!BsonClassMap.IsClassMapRegistered(typeof(Delivery))) BsonClassMap.RegisterClassMap<Delivery>(cm =>
        {
            cm.AutoMap();
            cm.MapIdProperty(x => x.Id).SetIdGenerator(new StringObjectIdGenerator());
        });
        // Entity
        if (!BsonClassMap.IsClassMapRegistered(typeof(TestEntity))) BsonClassMap.RegisterClassMap<Entity>(cm =>
        {
            cm.AutoMap();
            cm.MapIdProperty(gs => gs.Id)
              .SetIdGenerator(new StringObjectIdGenerator());
            cm.MapProperty(gs => gs.DomainEvents).SetSerializer(new ImmutableListSerializer<IDelivery<IEvent>>());
        });
        // GuildState
        if (!BsonClassMap.IsClassMapRegistered(typeof(GuildState))) BsonClassMap.RegisterClassMap<GuildState>(cm =>
        {
            cm.AutoMap();
            cm.MapProperty(gs => gs.SubscribedChannels).SetSerializer(new ImmutableListSerializer<SubscribedChannel>());
        });
    }

    [SetUp]
    public void SetUp()
    {
        _mongoClient = new MongoClient("mongodb://localhost:27017");
        _options = new OptionsWrapper<MongoSettings>(new MongoSettings
        {
            Database = Guid.NewGuid().ToString(),
        });
        _logger = NullLogger<MongoRepository<TestEntity>>.Instance;
    }

    [TearDown]
    public async Task TearDown()
    {
        var database = _mongoClient.GetDatabase(_options.Value.Database);
        var names    = await database.ListCollectionNamesAsync();

        foreach (var name in names.ToList())
        {
            await database.DropCollectionAsync(name);
        }
    }

    [Test]
    public async Task When_FindById_InstanceIsReturned()
    {
        // Arrange
        var database       = _mongoClient.GetDatabase(_options.Value.Database);
        var collectionName = "entities";
        var collection     = database.GetCollection<TestEntity>(collectionName);
        var expected       = new TestEntity("", ImmutableList<IDelivery<IEvent>>.Empty);
        await collection.InsertOneAsync(expected);

        var repository = Create(collectionName);

        // Act
        var actual = await repository.FindByIdAsync(expected.Id);

        // Assert
        actual.Should().Be(expected);
    }

    [Test]
    public async Task When_InsertAsync_IdIsGeneratedAndDocIsInserted()
    {
        // Arrange
        var database       = _mongoClient.GetDatabase(_options.Value.Database);
        var collectionName = "entities";
        var collection     = database.GetCollection<TestEntity>(collectionName);
        var doc            = new TestEntity("", ImmutableList<IDelivery<IEvent>>.Empty);

        var repository = Create(collectionName);

        // Act
        var actual = await repository.InsertAsync(doc);
        var saved  = await collection.Find(new BsonDocument("_id", doc.Id)).FirstOrDefaultAsync();

        // Assert
        actual.Id.Should().NotBeNull();
        actual.Id.Should().Be(saved.Id);
        actual.Should().Be(saved);
    }

    [Test]
    public async Task When_UpdateAsync_DocIsReplaced()
    {
        // Arrange
        var database       = _mongoClient.GetDatabase(_options.Value.Database);
        var collectionName = "entities";
        var collection     = database.GetCollection<TestEntity>(collectionName);
        var doc            = new TestEntity("", ImmutableList<IDelivery<IEvent>>.Empty);
        await collection.InsertOneAsync(doc);

        var repository = Create(collectionName);

        // Act
        var updated = doc with { Name = "TestEntity" };
        await repository.UpdateAsync(updated);
        var saved = await collection.Find(new BsonDocument("_id", doc.Id)).FirstOrDefaultAsync();

        // Assert
        saved.Should().Be(updated);
    }

    [Test]
    public async Task When_DeleteAsync_DocIsDeleted()
    {
        // Arrange
        var database       = _mongoClient.GetDatabase(_options.Value.Database);
        var collectionName = "entities";
        var collection     = database.GetCollection<TestEntity>(collectionName);
        var doc            = new TestEntity("", ImmutableList<IDelivery<IEvent>>.Empty);
        await collection.InsertOneAsync(doc);

        var repository = Create(collectionName);

        // Act
        await repository.DeleteAsync(doc.Id);
        var saved = await collection.Find(new BsonDocument("_id", doc.Id)).FirstOrDefaultAsync();

        // Assert
        saved.Should().BeNull();
    }

    // When_AddDomainEventAsync_EventIsAddedToDomainEvents
    [Test]
    public async Task When_AddDomainEventAsync_EventIsAddedToDomainEvents()
    {
        // Arrange
        var database       = _mongoClient.GetDatabase(_options.Value.Database);
        var collectionName = "entities";
        var collection     = database.GetCollection<TestEntity>(collectionName);
        var doc            = new TestEntity("", ImmutableList<IDelivery<IEvent>>.Empty);
        await collection.InsertOneAsync(doc);

        var repository = Create(collectionName);

        // Act
        var @event = Delivery.Of(new TestEvent("Hello"));
        await repository.AddDomainEventAsync(@event, doc.Id);
        var saved = await collection.Find(new BsonDocument("_id", doc.Id)).FirstOrDefaultAsync();

        // Assert
        saved.DomainEvents.Should().Contain(@event);
    }

    // When_RemoveDomainEventAsync_EventIsRemovedFromDomainEvents
    [Test]
    public async Task When_RemoveDomainEventAsync_EventIsRemovedFromDomainEvents()
    {
        // Arrange
        var database       = _mongoClient.GetDatabase(_options.Value.Database);
        var collectionName = "entities";
        var collection     = database.GetCollection<TestEntity>(collectionName);
        var doc            = new TestEntity("", ImmutableList<IDelivery<IEvent>>.Empty);
        await collection.InsertOneAsync(doc);

        var repository = Create(collectionName);

        // Act
        var @event = Delivery.Of(new TestEvent("Hello"));
        await repository.AddDomainEventAsync(@event, doc.Id);
        await repository.RemoveDomainEventAsync(@event, doc.Id);
        var saved = await collection.Find(new BsonDocument("_id", doc.Id)).FirstOrDefaultAsync();

        // Assert
        saved.DomainEvents.Should().NotContain(@event);
    }
}

public record TestEvent(string Name) : Event
{
}

public record TestEntity
    (string Name, ImmutableList<IDelivery<IEvent>> DomainEvents, string Id = "") : Entity(DomainEvents, Id)
{
}