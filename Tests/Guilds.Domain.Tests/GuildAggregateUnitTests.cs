using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Domain.Aggregates.GuildAggregate;
using NSubstitute;
using NUnit.Framework;

namespace Guilds.Domain.Tests;
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class GuildAggregateUnitTests
{
    private readonly IGuildsFactory _factory;
    private readonly IGuildService _guildService;
    private GuildsAggregate Create() => new(_factory, _guildService);

    public GuildAggregateUnitTests()
    {
        _factory = Substitute.For<IGuildsFactory>();
        _guildService = Substitute.For<IGuildService>();
    }

    [Test]
    public async Task When_GetGuildAsync_GuildsNotLoaded_NullReturned()
    {
        // Arrange
        var guildId = 123123123ul;
        var sut     = Create();

        // Act
        var actual = await sut.GetGuildAsync(guildId);

        // Assert
        actual.Should().BeNull();
    }
    
    [Test]
    public async Task When_GetGuildAsync_GuildLoaded_GuildReturned()
    {
        // Arrange
        var guildId    = 123123123ul;
        var sut        = Create();
        var expected      = Substitute.For<IGuildItem>();

        sut.AddGuild(guildId, expected);
        // Act
        var actual = await sut.GetGuildAsync(guildId);

        // Assert
        actual.Should().Be(expected);
    }
    
    [Test]
    public async Task When_LoadOrCreateAsync_GuildLoaded_And_GuildReturned()
    {
        // Arrange
        var guildId  = 123123123ul;
        var sut      = Create();
        var expected = Substitute.For<IGuildItem>();

        _factory.Create().Returns(expected);
        expected.LoadOrCreateStateAsync(Arg.Is(guildId)).Returns(Task.CompletedTask);
        // Act
        var actual = await sut.LoadOrCreateGuildAsync(guildId);

        // Assert
        actual.Should().Be(expected);
    }
}