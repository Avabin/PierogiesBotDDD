using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NUnit.Framework;
using Shared.Core.MessageBroker;
using Shared.Mongo.MongoRepository;

namespace Shared.Mongo.Tests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Integration")]
public class EventStoreUnitTests
{
    private readonly IMongoClient            _mongoClient;
    private readonly IOptions<MongoSettings> _options;
    private readonly ILogger<EventStore>     _logger;
    private          EventStore              Create() => new(_mongoClient, _options, _logger);

    private readonly IMongoDatabase             _database;
    private readonly IMongoCollection<Delivery> _collection;

    public EventStoreUnitTests()
    {
        _mongoClient = new MongoClient("mongodb://localhost:27017");
        _options = Options.Create<MongoSettings>(new MongoSettings
        {
            Database = Guid.NewGuid().ToString("N"),
        });
        _logger = NullLogger<EventStore>.Instance;

        _database   = _mongoClient.GetDatabase(_options.Value.Database);
        _collection = _database.GetCollection<Delivery>(EventStore.CollectionName);
    }

    [TearDown]
    public void TearDown()
    {
        _database.DropCollection(EventStore.CollectionName);
        _mongoClient.DropDatabase(_options.Value.Database);
    }

    [Test]
    public void When_Query_ReturnsCollectionAsQuery()
    {
        // Arrange
        var sut      = Create();
        var expected = _collection.AsQueryable();

        // Act
        var actual = sut.Query();

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task When_AddAsync_EventIsInserted()
    {
        // Arrange
        var sut = Create();
        var @event = new TestEvent(Guid.NewGuid().ToString());
        
        var correlationId = Guid.NewGuid();
        // Act
        await sut.AddAsync(Delivery.Of(@event, correlationId: correlationId));
        var actual = _collection.Find("{}").ToList().FirstOrDefault(x => x.CorrelationId == correlationId);
        
        // Assert
        actual.Should().NotBeNull();
        actual!.Data.Should().BeEquivalentTo(@event);
    }
}