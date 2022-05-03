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

[TestFixture]
[Category("Unit")]
public class QueryGuildHandlerUnitTests
{
    private IGuildsFactory                                          _guildsFactory;
    private IGuildService                                           _guildService;
    private Guilds.Domain.Aggregates.GuildAggregate.GuildsAggregate GetGuilds() => new(_guildsFactory);

    private Guilds.Domain.Aggregates.GuildAggregate.GuildItem GetGuild() => new(_guildService);

    [SetUp]
    public void SetUp()
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
        _guildService.LoadStateAsync(Arg.Is(guildId)).Returns(guildState);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().BeOfType<QueryGuildResult>();
    }
}