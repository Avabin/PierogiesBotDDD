using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Domain.Aggregates.GuildAggregate;
using Guilds.Mongo;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.Commands;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.Persistence;
using Shared.Guilds.Notifications;
using Shared.Mongo.MongoRepository;

namespace Guilds.Domain.Tests;
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class GuildServiceUnitTests
{
    private readonly IMongoRepositoryFactory _guildRepositoryFactory;
    private readonly IRepository<GuildState> _guildRepository;
    private readonly IMessageBroker          _messageBroker;
    private readonly IEventStore             _eventStore;
    private          GuildService            Create() => new(_guildRepositoryFactory, _messageBroker, _eventStore);

    public GuildServiceUnitTests()
    {
        _guildRepositoryFactory = Substitute.For<IMongoRepositoryFactory>();
        _guildRepository        = Substitute.For<IRepository<GuildState>>();
        _messageBroker          = Substitute.For<IMessageBroker>();
        _eventStore             = Substitute.For<IEventStore>();

        _guildRepositoryFactory.Create<GuildState>(Arg.Any<string>()).Returns(_guildRepository);
    }

    [Test]
    public async Task When_LoadState_ReturnsState()
    {
        // Arrange
        var guildService = Create();
        var guildId      = "123";
        var expected = new GuildState("Test Guild", 123123123ul, ImmutableList<SubscribedChannel>.Empty,
                                      ImmutableList<IDelivery<IEvent>>.Empty, guildId);

        _guildRepository.FindByIdAsync(Arg.Is(guildId)).Returns(expected);

        // Act
        var actual = await guildService.LoadOrCreateStateAsync(guildId);

        // Assert
        actual.Should().Be(expected);
        await _guildRepository.Received().FindByIdAsync(Arg.Is(guildId));
    }
    
    [Test]
    public async Task When_LoadStateSnowflakeId_ReturnsState()
    {
        // Arrange
        var guildService = Create();
        var guildId      = 123123123ul;
        var expected = new GuildState("Test Guild", guildId, ImmutableList<SubscribedChannel>.Empty,
                                      ImmutableList<IDelivery<IEvent>>.Empty, "123123");

        _guildRepository.FindOneByFieldAsync(Arg.Any<Expression<Func<GuildState, ulong>>>(),Arg.Is(guildId)).Returns(expected);

        // Act
        var actual = await guildService.LoadOrCreateStateAsync(guildId);

        // Assert
        actual.Should().Be(expected);
        await _guildRepository.Received().FindOneByFieldAsync(Arg.Any<Expression<Func<GuildState, ulong>>>(),Arg.Is(guildId));
    }

    [Test]
    public async Task When_LoadState_StateNotFound_CreatesNewState()
    {
        // Arrange
        var guildService = Create();
        var guildId      = "123";
        var expected     = GuildState.Empty with { Id = guildId };

        _guildRepository.FindByIdAsync(Arg.Is(guildId)).Returns(null as GuildState);
        _guildRepository.InsertAsync(Arg.Is(GuildState.Empty)).Returns(expected);

        // Act
        var actual = await guildService.LoadOrCreateStateAsync(guildId);

        // Assert
        actual.Should().Be(expected);
        await _guildRepository.Received().FindByIdAsync(Arg.Is(guildId));
        await _guildRepository.Received().InsertAsync(Arg.Is(GuildState.Empty));
    }

    [Test]
    public async Task When_LoadStateWithSnowflakeId_StateNotFound_CreatesNewState()
    {
        // Arrange
        var guildService = Create();
        var guildId      = 123123ul;
        var guildStateId = "123";
        var expected     = GuildState.Empty with { SnowflakeId = guildId, Id = guildStateId };

        _guildRepository.FindOneByFieldAsync(Arg.Any<Expression<Func<GuildState, ulong>>>(), Arg.Is(guildId))
                        .Returns(null as GuildState);
        _guildRepository.InsertAsync(Arg.Is(GuildState.Empty with { SnowflakeId = guildId })).Returns(expected);

        // Act
        var actual = await guildService.LoadOrCreateStateAsync(guildId);

        // Assert
        actual.Should().Be(expected);
        await _guildRepository.Received()
                              .FindOneByFieldAsync(Arg.Any<Expression<Func<GuildState, ulong>>>(), Arg.Is(guildId));
        await _guildRepository.Received().InsertAsync(Arg.Is(GuildState.Empty with { SnowflakeId = guildId }));
    }

    [Test]
    public async Task When_ChangeName_ReturnsNewStateAndNotifies()
    {
        // Arrange
        var guildService = Create();
        var guildStateId = "123";
        var guildId      = 123123123ul;
        var newName      = "New Name";
        var initialState = new GuildState("Test Guild", guildId, ImmutableList<SubscribedChannel>.Empty,
                                          ImmutableList<IDelivery<IEvent>>.Empty, guildStateId);
        var expected        = initialState with { Name = newName };
        var stateObservable = Observable.Return(initialState);

        _guildRepository.UpdateAsync(Arg.Is(expected)).Returns(Task.CompletedTask);

        // Act
        var actual = await guildService.ChangeNameAsync(newName, stateObservable);

        // Assert
        actual.Should().Be(expected);
        await _messageBroker.Received()
                            .NotifyAsync(Arg.Is<GuildNameChanged>(x => x.Name == newName), Arg.Is(guildId.ToString()));
        await _guildRepository.Received().UpdateAsync(Arg.Is(expected));
    }

    [Test]
    public async Task When_SubscribeChannel_ReturnsNewStateAndNotifies()
    {
        // Arrange
        var guildService = Create();
        var guildStateId = "123";
        var channelName  = "123123";
        var channelId    = 123123123ul;
        var guildId      = 123123123ul;
        var initialState = new GuildState("Test Guild", 123123123ul, ImmutableList<SubscribedChannel>.Empty,
                                          ImmutableList<IDelivery<IEvent>>.Empty, guildStateId);
        var subscribedChannel = new SubscribedChannel(channelName, channelId);
        var expected = initialState with
        {
            SubscribedChannels = new List<SubscribedChannel> { subscribedChannel }.ToImmutableList()
        };
        var stateObservable = Observable.Return(initialState);

        _guildRepository.UpdateAsync(Arg.Is(expected)).Returns(Task.CompletedTask);

        // Act
        var actual = await guildService.SubscribeChannelAsync(channelName, channelId, stateObservable);

        // Assert
        actual.SubscribedChannels.Should().BeEquivalentTo(expected.SubscribedChannels);
        await _messageBroker.Received()
                            .NotifyAsync(Arg.Is<SubscribedToChannel>(x => x.Name == channelName && x.ChannelId == channelId),
                                         Arg.Is(guildId.ToString()));
        await _guildRepository.Received()
                              .UpdateAsync(Arg.Is<GuildState>(state =>
                                                                  state.SubscribedChannels
                                                                       .Contains(subscribedChannel)));
    }

    [Test]
    public async Task When_UnsubscribeChannel_ReturnsNewStateAndNotifies()
    {
        // Arrange
        var guildService      = Create();
        var guildStateId      = "123";
        var channelName       = "123123";
        var channelId         = 123123123ul;
        var guildId           = 123123124ul;
        var subscribedChannel = new SubscribedChannel(channelName, channelId);
        var initialState = new GuildState("Test Guild", guildId,
                                          new List<SubscribedChannel> { subscribedChannel }.ToImmutableList(),
                                          ImmutableList<IDelivery<IEvent>>.Empty, guildStateId);
        var expected        = initialState with { SubscribedChannels = ImmutableList<SubscribedChannel>.Empty };
        var stateObservable = Observable.Return(initialState);

        _guildRepository.UpdateAsync(Arg.Is(expected)).Returns(Task.CompletedTask);

        // Act
        var actual = await guildService.UnsubscribeChannelAsync(channelId, stateObservable);

        // Assert
        actual.SubscribedChannels.Should().BeEquivalentTo(expected.SubscribedChannels);
        await _messageBroker.Received().NotifyAsync(Arg.Is<UnsubscribedFromChannel>(x => x.ChannelId == channelId),
                                                    Arg.Is(guildId.ToString()));
        await _guildRepository.Received().UpdateAsync(Arg.Is<GuildState>(state => state.SubscribedChannels.IsEmpty));
    }

    [Test]
    public async Task When_AddDomainEvent_EventIsAdded()
    {
        // Arrange
        var guildService = Create();
        var guildStateId = "123";
        var guildId      = 123123123ul;
        var command      = new Command();
        var delivery     = Delivery.Of(command);
        var initialState = new GuildState("Test Guild", guildId, ImmutableList<SubscribedChannel>.Empty,
                                          ImmutableList<IDelivery<IEvent>>.Empty, guildStateId);
        var expected        = initialState with { DomainEvents = initialState.DomainEvents.Add(delivery)};
        var stateObservable = Observable.Return(initialState);

        _guildRepository.UpdateAsync(Arg.Is(expected)).Returns(Task.CompletedTask);

        // Act
        var actual = await guildService.AddDomainEventAsync(delivery, stateObservable);

        // Assert
        actual.DomainEvents.First().Should().BeEquivalentTo(expected.DomainEvents.First());
        await _guildRepository.Received().UpdateAsync(Arg.Is<GuildState>(gs => gs.DomainEvents.Any(x => (Delivery) x == delivery)));
    }
    
    [Test]
    public async Task When_RemoveDomainEvent_EventIsRemoved()
    {
        // Arrange
        var guildService = Create();
        var guildStateId = "123";
        var guildId      = 123123123ul;
        var command      = new Command();
        var delivery     = Delivery.Of(command);
        var initialState = new GuildState("Test Guild", guildId, ImmutableList<SubscribedChannel>.Empty,
                                          ImmutableList<IDelivery<IEvent>>.Empty.Add(delivery), guildStateId);
        var expected        = initialState with { DomainEvents = initialState.DomainEvents.Remove(delivery)};
        var stateObservable = Observable.Return(initialState);

        _guildRepository.UpdateAsync(Arg.Is(expected)).Returns(Task.CompletedTask);

        // Act
        var actual = await guildService.RemoveDomainEventAsync(delivery, stateObservable);

        // Assert
        actual.DomainEvents.Should().BeEmpty();
        await _guildRepository.Received().UpdateAsync(Arg.Is<GuildState>(gs => gs.DomainEvents.IsEmpty));
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task When_Exists_ReturnsExpected(bool expected)
    {
        // Arrange
        var guildService = Create();
        var guildId      = 123123123ul;
        
        _guildRepository.ExistsAsync(Arg.Any<Expression<Func<GuildState, ulong>>>(), Arg.Is(guildId)).Returns(expected);

        // Act
        var actual = await guildService.ExistsAsync(guildId);

        // Assert
        actual.Should().Be(expected);
        await _guildRepository.Received().ExistsAsync(Arg.Any<Expression<Func<GuildState, ulong>>>(), Arg.Is(guildId));
    }
    
    [Test]
    public async Task When_DeleteState_AndExists_StateIsDeleted()
    {
        // Arrange
        var guildService  = Create();
        var guildId       = 123123123ul;
        var guildEntityId = "123";
        
        var initialState = new GuildState("Test Guild", guildId, ImmutableList<SubscribedChannel>.Empty,
                                          ImmutableList<IDelivery<IEvent>>.Empty, guildEntityId);
        var stateObservable = Observable.Return(initialState);
        
        _guildRepository.ExistsAsync(Arg.Any<Expression<Func<GuildState, ulong>>>(), Arg.Is(guildId)).Returns(true);

        // Act
        var actual = await guildService.DeleteStateAsync(stateObservable);

        // Assert
        actual.Should().Be(GuildState.Empty);
        await _guildRepository.Received().DeleteAsync(Arg.Is(guildEntityId));
    }
    
    [Test]
    public async Task When_DeleteState_AndDoesNotExists_NothingHappens()
    {
        // Arrange
        var guildService  = Create();
        var guildId       = 123123123ul;
        var guildEntityId = "123";
        
        var initialState = new GuildState("Test Guild", guildId, ImmutableList<SubscribedChannel>.Empty,
                                          ImmutableList<IDelivery<IEvent>>.Empty, guildEntityId);
        var stateObservable = Observable.Return(initialState);
        
        _guildRepository.ExistsAsync(Arg.Any<Expression<Func<GuildState, ulong>>>(), Arg.Is(guildId)).Returns(false);

        // Act
        var actual = await guildService.DeleteStateAsync(stateObservable);

        // Assert
        actual.Should().Be(initialState);
        await _guildRepository.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }
}

public record TestCommand() : Command;