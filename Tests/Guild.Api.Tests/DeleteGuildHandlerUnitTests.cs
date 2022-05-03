using System;
using System.Threading.Tasks;
using Guilds.Api.Commands;
using Guilds.Domain.Aggregates.GuildAggregate;
using NSubstitute;
using NUnit.Framework;
using Shared.Guilds.Commands;

namespace Guild.Api.Tests;
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category("Unit")]
public class DeleteGuildHandlerUnitTests
{
    private readonly IGuildsAggregate _guildsAggregate = Substitute.For<IGuildsAggregate>();
    private DeleteGuildHandler Create() => new(_guildsAggregate);

    [Test]
    public async Task When_HandleAsync_GuildIsDeleted()
    {
        // Arrange
        var sut     = Create();
        var guildId = 123123ul;
        var command = new DeleteGuild(guildId);
        
        _guildsAggregate.DeleteAsync(Arg.Is(guildId)).Returns(Task.CompletedTask);
        
        // Act
        await sut.HandleAsync(command);
        
        // Assert
        await _guildsAggregate.Received().DeleteAsync(guildId);
        
    }
}