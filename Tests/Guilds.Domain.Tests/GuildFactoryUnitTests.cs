using System;
using FluentAssertions;
using Guilds.Domain.Aggregates.GuildAggregate;
using Guilds.Mongo;
using NSubstitute;
using NUnit.Framework;

namespace Guilds.Domain.Tests;
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class GuildFactoryUnitTests
{
    private IServiceProvider _serviceProvider;
    private GuildsFactory Create() => new(_serviceProvider);

    public GuildFactoryUnitTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
    }

    [Test]
    public void When_Create_Returns_FromProvider()
    {
        // Arrange
        var factory = Create();
        var expected = Substitute.For<IGuildItem>();
        _serviceProvider.GetService(typeof(IGuildItem)).Returns(expected);
        
        // Act
        var actual = factory.Create();
        
        // Assert
        actual.Should().Be(expected);   
    }
}