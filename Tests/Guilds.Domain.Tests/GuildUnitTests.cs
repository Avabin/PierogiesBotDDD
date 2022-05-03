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

        _service.LoadStateAsync(Arg.Is(id)).Returns(expected);
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

        _service.LoadStateAsync(Arg.Is(id)).Returns(expected);
        await sut.LoadOrCreateStateAsync(id);
        // Act
        await sut.LoadOrCreateStateAsync(id);

        // Assert
        await _service.Received(1).LoadStateAsync(Arg.Any<string>());
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

        _service.LoadStateAsync(Arg.Is(id)).Returns(guildState);
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

        _service.LoadStateAsync(Arg.Is(id)).Returns(guildState);
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

        _service.LoadStateAsync(Arg.Is(id)).Returns(guildState);
        _service.UnsubscribeChannelAsync(Arg.Is(channelId), Arg.Any<IObservable<GuildState>>())
                .Returns(guildState with { SubscribedChannels = new List<SubscribedChannel> { }.ToImmutableList() });

        await sut.LoadOrCreateStateAsync(id);
        // Act
        await sut.UnsubscribeChannelAsync(channelId);
        var actual = await sut.StateObservable.FirstAsync();

        // Assert
        actual.SubscribedChannels.Should().NotContain(removed);
    }
}