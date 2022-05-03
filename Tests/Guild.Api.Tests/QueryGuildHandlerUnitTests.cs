using System.Collections.Immutable;
using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Api.Queries;
using Guilds.Domain.Aggregates.GuildAggregate;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
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
        var handler = new QueryGuildHandler(guilds);

        var guildState = new GuildState("Name", guildId, ImmutableList<SubscribedChannel>.Empty,
                                        ImmutableList<IDelivery<IEvent>>.Empty);

        _guildsFactory.Create().Returns(guild);
        _guildService.LoadOrCreateState(Arg.Is(guildId)).Returns(guildState);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().BeOfType<QueryGuildResult>();
    }
}