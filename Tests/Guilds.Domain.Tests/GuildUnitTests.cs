using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Guilds.Domain.Aggregates.GuildAggregate;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.Events;
using Shared.Core.MessageBroker;

namespace Guilds.Domain.Tests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class GuildUnitTests
{
    private readonly IGuildService _service;
    private          GuildItem     CreateGuild() => new(_service);

    public GuildUnitTests()
    {
        _service = Substitute.For<IGuildService>();
    }
    [Test]
    public async Task When_CreatedGuild_StateIsEmpty()
    {
        // Arrange
        var sut = CreateGuild();

        // Act
        var hasState = await sut.HasStateAsync();

        // Assert
        hasState.Should().BeFalse();
    }

    [Test]
    public async Task When_LoadState_StateIsPublished()
    {
        // Arrange
        var sut = CreateGuild();
        var id  = "1231";
        var expected = new GuildState("Old Nae", 123123123ul, ImmutableList<SubscribedChannel>.Empty,
                                      ImmutableList<IDelivery<IEvent>>.Empty, id);

        _service.LoadOrCreateStateAsync(Arg.Is(id)).Returns(expected);
        // Act
        await sut.LoadOrCreateStateAsync(id);
        var actual = await sut.StateObservable.FirstAsync();

        // Assert
        actual.Should().Be(expected);
    }

    [Test]
    public async Task When_LoadState_AndStateIsLoaded_NothingHappens()
    {
        // Arrange
        var sut = CreateGuild();
        var id  = "1232";
        var expected = new GuildState("Old Nae", 123123123ul, ImmutableList<SubscribedChannel>.Empty,
                                      ImmutableList<IDelivery<IEvent>>.Empty, id);

        _service.LoadOrCreateStateAsync(Arg.Is(id)).Returns(expected);
        await sut.LoadOrCreateStateAsync(id);
        // Act
        await sut.LoadOrCreateStateAsync(id);

        // Assert
        await _service.Received(1).LoadOrCreateStateAsync(Arg.Any<string>());
    }
    
    [Test]
    public async Task When_LoadStateWithSnowflakeId_AndStateIsLoaded_NothingHappens()
    {
        // Arrange
        var sut = CreateGuild();
        var id  = 1232ul;
        var expected = new GuildState("Old Nae", id, ImmutableList<SubscribedChannel>.Empty,
                                      ImmutableList<IDelivery<IEvent>>.Empty, "1231231");

        _service.LoadOrCreateStateAsync(Arg.Is(id)).Returns(expected);
        await sut.LoadOrCreateStateAsync(id);
        // Act
        await sut.LoadOrCreateStateAsync(id);

        // Assert
        await _service.DidNotReceive().LoadOrCreateStateAsync(Arg.Any<string>());
    }

    [Test]
    public async Task When_ChangeNameCalled_NewStateIsPublished()
    {
        // Arrange
        var sut      = CreateGuild();
        var id       = "1233";
        var expected = "New Name";
        var guildState = new GuildState("Old Nae", 123123123ul, ImmutableList<SubscribedChannel>.Empty,
                                        ImmutableList<IDelivery<IEvent>>.Empty, id);

        _service.LoadOrCreateStateAsync(Arg.Is(id)).Returns(guildState);
        _service.ChangeNameAsync(Arg.Any<string>(), Arg.Any<IObservable<GuildState>>())
                .Returns(guildState with { Name = expected });

        await sut.LoadOrCreateStateAsync(id);

        // Act
        await sut.ChangeNameAsync(expected);
        var result = await sut.StateObservable.FirstAsync();
        var actual = result.Name;

        // Assert
        actual.Should().Be(expected);
    }

    [Test]
    public async Task When_SubscribeChannel_NewStateIsPublished()
    {
        // Arrange
        var sut      = CreateGuild();
        var id       = "1234";
        var expected = new SubscribedChannel("channel", 123ul);
        var guildState = new GuildState("Old Nae", 123123123ul, ImmutableList<SubscribedChannel>.Empty,
                                        ImmutableList<IDelivery<IEvent>>.Empty, id);

        _service.LoadOrCreateStateAsync(Arg.Is(id)).Returns(guildState);
        _service.SubscribeChannelAsync(Arg.Is(expected.Name), Arg.Is(expected.ChannelId),
                                       Arg.Any<IObservable<GuildState>>())
                .Returns(guildState with
                 {
                     SubscribedChannels = new List<SubscribedChannel> { expected }.ToImmutableList()
                 });

        await sut.LoadOrCreateStateAsync(id);
        // Act
        await sut.SubscribeChannelAsync(expected.Name, expected.ChannelId);
        var actual = await sut.StateObservable.FirstAsync();

        // Assert
        actual.SubscribedChannels.Should().Contain(expected);
    }

    [Test]
    public async Task When_UnsubscribeChannel_NewStateIsPublished()
    {
        // Arrange
        var sut        = CreateGuild();
        var id         = "1235";
        var channelId  = 123ul;
        var removed    = new SubscribedChannel("channel", channelId);
        var channels   = new List<SubscribedChannel> { removed }.ToImmutableList();
        var guildState = new GuildState("Old Nae", 123123123ul, channels, ImmutableList<IDelivery<IEvent>>.Empty, id);

        _service.LoadOrCreateStateAsync(Arg.Is(id)).Returns(guildState);
        _service.UnsubscribeChannelAsync(Arg.Is(channelId), Arg.Any<IObservable<GuildState>>())
                .Returns(guildState with { SubscribedChannels = new List<SubscribedChannel> { }.ToImmutableList() });

        await sut.LoadOrCreateStateAsync(id);
        // Act
        await sut.UnsubscribeChannelAsync(channelId);
        var actual = await sut.StateObservable.FirstAsync();

        // Assert
        actual.SubscribedChannels.Should().NotContain(removed);
    }

    [Test]
    public async Task When_DeleteStateAsync_StateNotLoaded_NothingHappens()
    {
        // Arrange
        var sut         = CreateGuild();
        
        // Act
        await sut.DeleteStateAsync();

        // Assert
        await _service.DidNotReceive().DeleteStateAsync(Arg.Any<IObservable<GuildState>>());
    }
    
    [Test]
    public async Task When_DeleteStateAsync_StateLoaded_StateDeleted()
    {
        // Arrange
        var sut        = CreateGuild();
        var guildId    = 123123123ul;
        var guildState = GuildState.Empty with {SnowflakeId = guildId, Id = "123123"};
        
        _service.LoadOrCreateStateAsync(Arg.Is(guildId)).Returns(guildState);
        
        await sut.LoadOrCreateStateAsync(guildId);
        // Act
        await sut.DeleteStateAsync();

        // Assert
        await _service.Received().DeleteStateAsync(Arg.Any<IObservable<GuildState>>());
    }

    [Test]
    public async Task When_AddDomainEvent_StateNotLoaded_NothingHappens()
    {
        // Arrange
        var sut         = CreateGuild();

        var delivery = Delivery.Of(new TestCommand());
        // Act
        await sut.AddDomainEventAsync(delivery);
        
        // Assert
        await _service.DidNotReceive().AddDomainEventAsync(delivery, Arg.Any<IObservable<GuildState>>());
    }
    
    [Test]
    public async Task When_AddDomainEvent_StateLoaded_EventAdded()
    {
        // Arrange
        var guildId = 123123123ul;
        var sut     = CreateGuild();
        var guildState = GuildState.Empty with {SnowflakeId = guildId, Id = "123123"};

        _service.LoadOrCreateStateAsync(Arg.Is(guildId)).Returns(guildState);

        await sut.LoadOrCreateStateAsync(guildId);
        var delivery = Delivery.Of(new TestCommand());
        // Act
        await sut.AddDomainEventAsync(delivery);
        
        // Assert
        await _service.Received().AddDomainEventAsync(delivery, Arg.Any<IObservable<GuildState>>());
    }
    
    [Test]
    public async Task When_RemoveDomainEvent_StateNotLoaded_NothingHappens()
    {
        // Arrange
        var sut = CreateGuild();

        var delivery = Delivery.Of(new TestCommand());
        // Act
        await sut.RemoveDomainEventAsync(delivery);
        
        // Assert
        await _service.DidNotReceive().RemoveDomainEventAsync(delivery, Arg.Any<IObservable<GuildState>>());
    }
    
    [Test]
    public async Task When_RemoveDomainEvent_StateLoaded_EventAdded()
    {
        // Arrange
        var guildId    = 123123123ul;
        var sut        = CreateGuild();
        var delivery = Delivery.Of(new TestCommand());
        var guildState = GuildState.Empty with {SnowflakeId = guildId, Id = "123123", DomainEvents = ImmutableList.Create<IDelivery<IEvent>>(delivery)};

        _service.LoadOrCreateStateAsync(Arg.Is(guildId)).Returns(guildState);

        await sut.LoadOrCreateStateAsync(guildId);
        // Act
        await sut.RemoveDomainEventAsync(delivery);
        
        // Assert
        await _service.Received().RemoveDomainEventAsync(delivery, Arg.Any<IObservable<GuildState>>());
    }
}