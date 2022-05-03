using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.SeedWork;
using Shared.Mongo.MongoRepository;

namespace Shared.Mongo.Tests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class MongoRepositoryFactoryUnitTests
{
    private readonly IServiceProvider        _serviceProvider;

    public MongoRepositoryFactoryUnitTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        var client  = Substitute.For<IMongoClient>();
        var          settings = Substitute.For<IOptions<MongoSettings>>();

        _serviceProvider.GetService<IMongoClient>().Returns(client);
        _serviceProvider.GetService<IOptions<MongoSettings>>().Returns(settings);
        _serviceProvider.GetService<ILogger<MongoRepository<Entity>>>()
                        .Returns(NullLogger<MongoRepository<Entity>>.Instance);
    }

    [Test]
    public void When_Create_ReturnsNewInstance()
    {
        // Arrange
        var sut      = new MongoRepositoryFactory(_serviceProvider);
        var expected = "entities";


        // Act
        var result = sut.Create<Entity>(expected);
        var actual = (result as MongoRepository<Entity>)?.CollectionName;

        // Assert
        result.Should().NotBeNull();
        actual.Should().Be(expected);
    }
}