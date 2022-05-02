using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.SeedWork;
using Shared.Mongo.MongoRepository;

namespace Shared.Mongo.Tests;

[TestFixture]
[Category("Unit")]
public class MongoRepositoryFactoryUnitTests
{
    private IServiceProvider _serviceProvider;
    private IMongoClient     _client;
    private IOptions<MongoSettings> _settings;
    

    [SetUp]
    public void SetUp()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _client          = Substitute.For<IMongoClient>();
        _settings        = Substitute.For<IOptions<MongoSettings>>();

        _serviceProvider.GetService<IMongoClient>().Returns(_client);
        _serviceProvider.GetService<IOptions<MongoSettings>>().Returns(_settings);
    }

    [Test]
    public void When_Create_ReturnsNewInstance()
    {
        // Arrange
        var sut = new MongoRepositoryFactory(_serviceProvider);
        var expected       = "entities";
        
        
        // Act
        var result         = sut.Create<Entity>(expected);
        var actual = (result as MongoRepository<Entity>)?.CollectionName;
        
        // Assert
        result.Should().NotBeNull();
        actual.Should().Be(expected);
    }
}