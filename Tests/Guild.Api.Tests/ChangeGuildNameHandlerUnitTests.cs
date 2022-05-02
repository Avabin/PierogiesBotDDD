using System;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Api.Commands;
using Guilds.Domain.Aggregates.GuildAggregate;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Guilds.Commands;

namespace Guild.Api.Tests;

[TestFixture]
[Category("Unit")]
public class ChangeGuildNameHandlerUnitTests
{
    private IGuildsFactory                                 _guildsFactory;
    private IGuildService                                  _service;
    private Guilds.Domain.Aggregates.GuildAggregate.GuildItem  CreateGuild()  => new(_service);
    private Guilds.Domain.Aggregates.GuildAggregate.GuildsAggregate CreateGuilds() => new(_guildsFactory);
    [SetUp]
    public void Setup()
    {
        _guildsFactory = Substitute.For<IGuildsFactory>();
        _service = Substitute.For<IGuildService>();
    }

    [Test]
    public async Task When_HandleAsync_ServiceIsCalled_And_ObserversAreNotified()
    {
        // Arrange
        var guild    = CreateGuild();
        var guilds = CreateGuilds();
        var guildId  = 123123123ul;
        var expected = "newName";
        var guildState = new GuildState("Guild", guildId, ImmutableList<SubscribedChannel>.Empty,
                                        ImmutableList<IDelivery<IEvent>>.Empty);
        
        _guildsFactory.Create().Returns(guild);
        
        _service.LoadStateAsync(Arg.Is(guildId)).Returns(guildState);
        _service.ChangeNameAsync(Arg.Is(expected), Arg.Any<IObservable<GuildState>>())
                .Returns(guildState with { Name = expected });
        var handler = new ChangeGuildNameHandler(guilds);
        var command = new ChangeGuildName(expected, guildId);

        // Act
        await handler.HandleAsync(command);

        var actual = await guild.StateObservable.Select(x => x.Name).FirstAsync();
        
        // Assert
        actual.Should().Be(expected);

    }
}