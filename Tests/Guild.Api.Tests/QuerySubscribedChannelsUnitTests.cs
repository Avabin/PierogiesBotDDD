using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Api.Queries;
using Guilds.Domain.Aggregates.GuildAggregate;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.Queries;
using Shared.Guilds.Queries;

namespace Guild.Api.Tests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class QuerySubscribedChannelsUnitTests
{
    private readonly IGuildsAggregate               _guildsAggregate;
    private          QuerySubscribedChannelsHandler Create() => new QuerySubscribedChannelsHandler(_guildsAggregate);

    public QuerySubscribedChannelsUnitTests()
    {
        _guildsAggregate = Substitute.For<IGuildsAggregate>();
    }

    [Test]
    public async Task When_HandleAsync_GuildExists_ChannelsAreReturned()
    {
        // Arrange
        var guildId = 123123ul;
        var query   = new QuerySubscribedChannels(guildId);
        var handler = Create();
        var channels = Enumerable.Range(0, 10).Select(i => new SubscribedChannel($"Channel {i}", (ulong)(i * 1006171L)))
                                 .ToImmutableList();

        var guildState      = new GuildState("Guild", guildId, channels);
        var stateObservable = Observable.Return(guildState);
        var guild           = Substitute.For<IGuildItem>();
        guild.StateObservable.Returns(stateObservable);
        _guildsAggregate.GetGuildAsync(Arg.Is(guildId)).Returns(guild);
        // Act
        var actual = await handler.HandleAsync(query);

        // Assert
        actual.Should().BeOfType<QuerySubscribedChannelsResult>();
        (actual as QuerySubscribedChannelsResult)?.Channels.Should().BeEquivalentTo(channels);
    }
    
    [Test]
    public async Task When_HandleAsync_GuildNotExists_EmptyResultReturned()
    {
        // Arrange
        var guildId = 123123ul;
        var query   = new QuerySubscribedChannels(guildId);
        var handler = Create();

        var guildState      = GuildState.Empty;
        _guildsAggregate.GetGuildAsync(Arg.Is(guildId)).Returns(Task.FromResult<IGuildItem?>(null));
        // Act
        var actual = await handler.HandleAsync(query);

        // Assert
        actual.Should().BeOfType<QuerySubscribedChannelsResult>();
        (actual as QuerySubscribedChannelsResult)?.Should().Be(QuerySubscribedChannelsResult.Empty);
    }

    [Test]
    public async Task When_HandleAsync_AsBase_GuildExists_ChannelsAreReturned()
    {
        // Arrange
        var           guildId = 123123ul;
        var           query   = new QuerySubscribedChannels(guildId);
        IQueryHandler handler = Create();
        var channels = Enumerable.Range(0, 10).Select(i => new SubscribedChannel($"Channel {i}", (ulong)(i * 1006171L)))
                                 .ToImmutableList();

        var guildState      = new GuildState("Guild", guildId, channels);
        var stateObservable = Observable.Return(guildState);
        var guild           = Substitute.For<IGuildItem>();
        guild.StateObservable.Returns(stateObservable);
        _guildsAggregate.GetGuildAsync(Arg.Is(guildId)).Returns(guild);
        // Act
        var actual = await handler.HandleAsync(query);

        // Assert
        actual.Should().BeOfType<QuerySubscribedChannelsResult>();
        (actual as QuerySubscribedChannelsResult)?.Channels.Should().BeEquivalentTo(channels);
    }
}