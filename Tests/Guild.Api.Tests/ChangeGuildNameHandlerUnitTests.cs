using System;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Api.Commands;
using Guilds.Domain.Aggregates.GuildAggregate;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.Commands;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Guilds.Commands;

namespace Guild.Api.Tests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class ChangeGuildNameHandlerUnitTests
{
    private IGuildsFactory                                    _guildsFactory;
    private IGuildService                                     _service;
    private IGuildsAggregate                                  _guildsAggregate;
    private GuildItem CreateGuild() => new(_service);

    [SetUp]
    public void Setup()
    {
        _guildsFactory   = Substitute.For<IGuildsFactory>();
        _service         = Substitute.For<IGuildService>();
        _guildsAggregate = Substitute.For<IGuildsAggregate>();
    }

    [Test]
    public async Task When_HandleAsync_NameTheSame_NothingHappens()
    {
        // Arrange
        var guild      = CreateGuild();
        var guildName  = "Guild";
        var guildId    = 123123ul;
        var guildState = GuildState.Empty with { Name = guildName, SnowflakeId = guildId};
        var command    = new ChangeGuildNameCommand(guildName, guildId);
        var handler    = new ChangeGuildNameHandler(_guildsAggregate);
        
        _guildsAggregate.GetGuildAsync(Arg.Is(guildId)).Returns(guild);
        _service.LoadOrCreateStateAsync(Arg.Is(guildId)).Returns(guildState);
        await guild.LoadOrCreateStateAsync(guildId);
        // Act
        await handler.HandleAsync(command);
        
        // Assert
        await _service.DidNotReceive().ChangeNameAsync(Arg.Any<string>(), Arg.Any<IObservable<GuildState>>());
        await _service.DidNotReceive().AddDomainEventAsync(Arg.Any<IDelivery<IEvent>>(), Arg.Any<IObservable<GuildState>>());
    }

    [Test]
    public async Task When_HandleAsync_ServiceIsCalled_And_ObserversAreNotified()
    {
        // Arrange
        var guild    = CreateGuild();
        var guildId  = 123123123ul;
        var expected = "newName";
        var guildState = new GuildState("Guild", guildId, ImmutableList<SubscribedChannel>.Empty,
                                        ImmutableList<IDelivery<IEvent>>.Empty, "123123");

        _guildsFactory.Create().Returns(guild);
        _guildsAggregate.GetGuildAsync(Arg.Is(guildId)).Returns(guild);

        _service.LoadOrCreateStateAsync(Arg.Is(guildId)).Returns(guildState);
        _service.ChangeNameAsync(Arg.Is(expected), Arg.Any<IObservable<GuildState>>())
                .Returns(guildState with { Name = expected });
        _service.AddDomainEventAsync(Arg.Any<IDelivery<IEvent>>(), Arg.Any<IObservable<GuildState>>())
                .Returns(async info =>
                 {
                     var arg      = info.Arg<IObservable<GuildState>>();
                     var delivery = info.Arg<IDelivery<IEvent>>();
                     var state    = await arg.FirstAsync();

                     await Task.Delay(1);
                     return state with { DomainEvents = state.DomainEvents.Add(delivery) };
                 });
        await guild.LoadOrCreateStateAsync(guildId);
        ICommandHandler handler = new ChangeGuildNameHandler(_guildsAggregate);
        var command = new ChangeGuildNameCommand(expected, guildId);

        handler.Context = Delivery.Of(command);
        // Act
        await handler.HandleAsync(command);
        var actual = await guild.StateObservable.FirstAsync();

        // Assert
        actual.Name.Should().Be(expected);
        actual.DomainEvents.Should().Contain(x => (x.Data as ChangeGuildNameCommand) == command);
    }
}