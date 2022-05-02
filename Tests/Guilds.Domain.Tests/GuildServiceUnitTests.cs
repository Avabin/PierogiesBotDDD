using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Domain.Aggregates.GuildAggregate;
using Guilds.Mongo;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.Persistence;
using Shared.Guilds.Notifications;
using Shared.Mongo.MongoRepository;

namespace Guilds.Domain.Tests;

[TestFixture]
[Category("Unit")]
public class GuildServiceUnitTests
{
    private IMongoRepositoryFactory _guildRepositoryFactory;
    private IRepository<GuildState> _guildRepository;
    private IMessageBroker _messageBroker;
    private IEventStore _eventStore;
    private GuildService Create() => new(_guildRepositoryFactory, _messageBroker, _eventStore);
    
    [SetUp]
    public void SetUp()
    {
        _guildRepositoryFactory = Substitute.For<IMongoRepositoryFactory>();
        _guildRepository = Substitute.For<IRepository<GuildState>>();
        _messageBroker   = Substitute.For<IMessageBroker>();
        _eventStore      = Substitute.For<IEventStore>();
        
        _guildRepositoryFactory.Create<GuildState>(Arg.Any<string>()).Returns(_guildRepository);
    }

    [Test]
    public async Task When_LoadState_ReturnsState()
    {
        // Arrange
        var guildService = Create();
        var guildId      = "123";
        var expected   = new GuildState("Test Guild", 123123123ul, ImmutableList<SubscribedChannel>.Empty, ImmutableList<IDelivery<IEvent>>.Empty, guildId);
        
        _guildRepository.FindByIdAsync(Arg.Is(guildId)).Returns(expected);
        
        // Act
        var actual = await guildService.LoadStateAsync(guildId);
        
        // Assert
        actual.Should().Be(expected);
        await _guildRepository.Received().FindByIdAsync(Arg.Is(guildId));
    }
    
    [Test]
    public async Task When_LoadState_StateNotFound_CreatesNewState()
    {
        // Arrange
        var guildService = Create();
        var guildId      = "123";
        var expected     = GuildState.Empty with {Id = guildId};

        _guildRepository.FindByIdAsync(Arg.Is(guildId)).Returns(null as GuildState);
        _guildRepository.InsertAsync(Arg.Is(GuildState.Empty)).Returns(expected);

        // Act
        var actual = await guildService.LoadStateAsync(guildId);
        
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
        var expected     = GuildState.Empty with {SnowflakeId = guildId, Id = guildStateId};

        _guildRepository.FindOneByFieldAsync(Arg.Any<Expression<Func<GuildState, ulong>>>(),Arg.Is(guildId)).Returns(null as GuildState);
        _guildRepository.InsertAsync(Arg.Is(GuildState.Empty with {SnowflakeId = guildId})).Returns(expected);

        // Act
        var actual = await guildService.LoadStateAsync(guildId);
        
        // Assert
        actual.Should().Be(expected);
        await _guildRepository.Received().FindOneByFieldAsync(Arg.Any<Expression<Func<GuildState, ulong>>>(),Arg.Is(guildId));
        await _guildRepository.Received().InsertAsync(Arg.Is(GuildState.Empty with {SnowflakeId = guildId}));
    }

    [Test]
    public async Task When_ChangeName_ReturnsNewStateAndNotifies()
    {
        // Arrange
        var guildService = Create();
        var guildStateId = "123";
        var guildId = 123123123ul;
        var newName = "New Name";
        var initialState = new GuildState("Test Guild", guildId, ImmutableList<SubscribedChannel>.Empty, ImmutableList<IDelivery<IEvent>>.Empty, guildStateId);
        var expected     = initialState with {Name = newName};
        var stateObservable = Observable.Return(initialState);
        
        _guildRepository.UpdateAsync(Arg.Is(expected)).Returns(Task.CompletedTask);
        
        // Act
        var actual = await guildService.ChangeNameAsync(newName, stateObservable);
        
        // Assert
        actual.Should().Be(expected);
        await _messageBroker.Received().NotifyAsync(Arg.Is<GuildNameChanged>(x => x.Name == newName), Arg.Is(guildId.ToString()));
        await _guildRepository.Received().UpdateAsync(Arg.Is(expected));
    }

    [Test]
    public async Task When_SubscribeChannel_ReturnsNewStateAndNotifies()
    {
        // Arrange
        var guildService      = Create();
        var guildStateId           = "123";
        var channelName       = "123123";
        var channelId         = 123123123ul;
        var guildId = 123123123ul;
        var initialState      = new GuildState( "Test Guild", 123123123ul, ImmutableList<SubscribedChannel>.Empty, ImmutableList<IDelivery<IEvent>>.Empty, guildStateId);
        var subscribedChannel = new SubscribedChannel(channelName, channelId);
        var expected          = initialState with {SubscribedChannels = new List<SubscribedChannel> { subscribedChannel }.ToImmutableList()};
        var stateObservable   = Observable.Return(initialState);
        
        _guildRepository.UpdateAsync(Arg.Is(expected)).Returns(Task.CompletedTask);
        
        // Act
        var actual = await guildService.SubscribeChannelAsync( channelName, channelId, stateObservable);
        
        // Assert
        actual.SubscribedChannels.Should().BeEquivalentTo(expected.SubscribedChannels);
        await _messageBroker.Received().NotifyAsync(Arg.Is<SubscribedToChannel>(x => x.Name == channelName && x.ChannelId == channelId), Arg.Is(guildId.ToString()));
        await _guildRepository.Received().UpdateAsync(Arg.Is<GuildState>(state => state.SubscribedChannels.Contains(subscribedChannel)));
    }
    
    [Test]
    public async Task When_UnsubscribeChannel_ReturnsNewStateAndNotifies()
    {
        // Arrange
        var guildService      = Create();
        var guildStateId           = "123";
        var channelName       = "123123";
        var channelId         = 123123123ul;
        var guildId = 123123123ul;
        var subscribedChannel = new SubscribedChannel(channelName, channelId);
        var initialState      = new GuildState( "Test Guild", 123123123ul, new List<SubscribedChannel> { subscribedChannel }.ToImmutableList(), ImmutableList<IDelivery<IEvent>>.Empty, guildStateId);
        var expected          = initialState with {SubscribedChannels = ImmutableList<SubscribedChannel>.Empty};
        var stateObservable   = Observable.Return(initialState);
        
        _guildRepository.UpdateAsync(Arg.Is(expected)).Returns(Task.CompletedTask);
        
        // Act
        var actual = await guildService.UnsubscribeChannelAsync(channelId, stateObservable);
        
        // Assert
        actual.SubscribedChannels.Should().BeEquivalentTo(expected.SubscribedChannels);
        await _messageBroker.Received().NotifyAsync(Arg.Is<UnsubscribedFromChannel>(x => x.ChannelId == channelId), Arg.Is(guildId.ToString()));
        await _guildRepository.Received().UpdateAsync(Arg.Is<GuildState>(state => state.SubscribedChannels.IsEmpty));
    }
}