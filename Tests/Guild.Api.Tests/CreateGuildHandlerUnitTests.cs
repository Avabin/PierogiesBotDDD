using System.Threading.Tasks;
using Guilds.Api.Commands;
using Guilds.Domain.Aggregates.GuildAggregate;
using NSubstitute;
using NUnit.Framework;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Guilds.Commands;

namespace Guild.Api.Tests;
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class CreateGuildHandlerUnitTests
{
    private readonly IGuildsAggregate   _guildsAggregate = Substitute.For<IGuildsAggregate>();
    private CreateGuildHandler Create() => new(_guildsAggregate);

    [Test]
    public async Task When_HandleAsync_GuildNotFound_GuildCreated_And_EventStored()
    {
        // Arrange
        var sut     = Create();
        var command = new CreateGuildCommand("Test Guild", 123123ul);
        var guild   = Substitute.For<IGuildItem>();

        _guildsAggregate.GetGuildAsync(Arg.Any<ulong>())!.Returns(Task.FromResult<IGuildItem>(null!));
        guild.ChangeNameAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        guild.AddDomainEventAsync(Arg.Any<IDelivery<IEvent>>()).Returns(Task.CompletedTask);
        _guildsAggregate.LoadOrCreateGuildAsync(Arg.Any<ulong>()).Returns(Task.FromResult(guild));
        sut.Context = Delivery.Of(command);
        // Act
        await sut.HandleAsync(command);

        // Assert
        await _guildsAggregate.Received(1).LoadOrCreateGuildAsync(command.SnowflakeId);
        await guild.Received(1).AddDomainEventAsync(Arg.Any<IDelivery<IEvent>>());
        await guild.Received(1).ChangeNameAsync(Arg.Is(command.Name));
    }

    [Test]
    public async Task When_HandleAsync_GuildFound_NothingHappens()
    {
        // Arrange
        var sut     = Create();
        var command = new CreateGuildCommand("Test Guild", 123123ul);
        var guild   = Substitute.For<IGuildItem>();

        _guildsAggregate.GetGuildAsync(Arg.Any<ulong>())!.Returns(Task.FromResult(guild));
        _guildsAggregate.LoadOrCreateGuildAsync(Arg.Any<ulong>()).Returns(Task.FromResult(guild));
        sut.Context = Delivery.Of(command);
        // Act
        await sut.HandleAsync(command);

        // Assert
        await _guildsAggregate.Received(0).LoadOrCreateGuildAsync(command.SnowflakeId);
        await guild.Received(0).AddDomainEventAsync(Arg.Any<IDelivery<IEvent>>());
        await guild.Received(0).ChangeNameAsync(Arg.Is(command.Name));
    }
}