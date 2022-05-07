using System.Reactive.Linq;
using System.Threading.Tasks;
using Guilds.Api.Commands;
using Guilds.Domain.Aggregates.GuildAggregate;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using Shared.Core.MessageBroker;
using Shared.Guilds.Commands;

namespace Guild.Api.Tests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class UnsubscribeChannelHandlerUnitTests
{
    private readonly IGuildsAggregate        _guildsAggregate = Substitute.For<IGuildsAggregate>();
    private          UnsubscribeChannelHandler Create() => new(_guildsAggregate);


    [Test]
    public async Task When_HandleAsync_ChannelIsAdded()
    {
        // Arrange
        var sut       = Create();
        var channelId = 123123123ul;
        var guildId   = 123123123ul;
        var command   = new UnsubscribeChannelCommand(channelId, guildId);
        var guild     = Substitute.For<IGuildItem>();
        var guildState = new GuildState("Guild", guildId);
        var stateObservable = Observable.Return(guildState);

        guild.StateObservable.Returns(stateObservable);
        _guildsAggregate.GetGuildAsync(Arg.Is(guildId)).Returns(guild);

        var delivery = Delivery.Of(command);
        sut.Context = delivery;
        // Act
        await sut.HandleAsync(command);
        
        // Assert
        await guild.Received().UnsubscribeChannelAsync(Arg.Is(channelId));
        await guild.AddDomainEventAsync(Arg.Is(delivery));
    }
    
    [Test]
    public async Task When_HandleAsync_GuildNotExists_NothingHappens()
    {
        // Arrange
        var sut             = Create();
        var channelId       = 123123123ul;
        var guildId         = 123123123ul;
        var command         = new UnsubscribeChannelCommand(channelId, guildId);
        var guild           = Substitute.For<IGuildItem>();

        _guildsAggregate.GetGuildAsync(Arg.Any<ulong>()).ReturnsNull();
        // Act
        await sut.HandleAsync(command);
        
        // Assert
        await guild.DidNotReceive().UnsubscribeChannelAsync(Arg.Any<ulong>());
    }
}