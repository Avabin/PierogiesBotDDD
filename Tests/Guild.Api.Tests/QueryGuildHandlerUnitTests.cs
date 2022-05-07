using System.Collections.Immutable;
using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Api.Extensions;
using Guilds.Api.Queries;
using Guilds.Domain.Aggregates.GuildAggregate;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.Queries;
using Shared.Guilds.Queries;

namespace Guild.Api.Tests;
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class QueryGuildHandlerUnitTests
{
    private readonly IGuildsFactory  _guildsFactory;
    private readonly IGuildService   _guildService;
    private          GuildsAggregate GetGuilds() => new(_guildsFactory, _guildService);

    private GuildItem GetGuild() => new(_guildService);


    public QueryGuildHandlerUnitTests()
    {
        _guildsFactory = Substitute.For<IGuildsFactory>();
        _guildService  = Substitute.For<IGuildService>();
    }

    [Test]
    public async Task When_HandleAsync_GuildIsFetched_AndViewIsReturned()
    {
        // Arrange
        var guildId = 123123123ul;
        var guilds  = GetGuilds();
        var guild   = GetGuild();
        var query   = new QueryGuild(guildId);
        IQueryHandler handler = new QueryGuildHandler(guilds);

        var guildState = new GuildState("Name", guildId, ImmutableList<SubscribedChannel>.Empty,
                                        ImmutableList<IDelivery<IEvent>>.Empty);

        _guildsFactory.Create().Returns(guild);
        _guildService.ExistsAsync(Arg.Is(guildId)).Returns(true);
        _guildService.LoadOrCreateStateAsync(Arg.Is(guildId)).Returns(guildState);

        // Act
        var result = (QueryGuildResult) await handler.HandleAsync(query);

        // Assert
        result.Should().BeOfType<QueryGuildResult>();
        result.Guild.SnowflakeId.Should().Be(guildId);
    }
    
    [Test]
    public async Task When_HandleAsync_GuildNotFound_ResultWithEmptyViewIsReturned()
    {
        // Arrange
        var           guildId = 123123123ul;
        var           guilds  = GetGuilds();
        var           guild   = GetGuild();
        var           query   = new QueryGuild(guildId);
        IQueryHandler handler = new QueryGuildHandler(guilds);

        _guildService.ExistsAsync(Arg.Is(guildId)).Returns(false);

        // Act
        var actual = (QueryGuildResult) await handler.HandleAsync(query);

        // Assert
        actual.Should().BeOfType<QueryGuildResult>();
        actual.Guild.Should().Be(GuildState.Empty.ToView());
    }

    [Test]
    public async Task When_HandleAsync_AsBase_GuildIsFetched_AndViewIsReturned()
    {
        // Arrange
        var           guildId = 123123123ul;
        var           guilds  = GetGuilds();
        var           guild   = GetGuild();
        IQuery           query   = new QueryGuild(guildId);
        IQueryHandler handler = new QueryGuildHandler(guilds);

        var guildState = new GuildState("Name", guildId, ImmutableList<SubscribedChannel>.Empty,
                                        ImmutableList<IDelivery<IEvent>>.Empty);

        _guildsFactory.Create().Returns(guild);
        
        _guildService.ExistsAsync(Arg.Is(guildId)).Returns(true);
        _guildService.LoadOrCreateStateAsync(Arg.Is(guildId)).Returns(guildState);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().BeOfType<QueryGuildResult>();
    }
}