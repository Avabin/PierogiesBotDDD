using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Shared.Core.SeedWork;

[assembly: InternalsVisibleTo("Guilds.Domain.Tests")]
[assembly: InternalsVisibleTo("Guild.Api.Tests")]
namespace Guilds.Domain.Aggregates.GuildAggregate;

internal class GuildsAggregate : IAggregateRoot, IGuildsAggregate
{
    private readonly IGuildsFactory                          _guildsFactory;
    private readonly ConcurrentDictionary<ulong, IGuildItem> _guilds = new();

    public GuildsAggregate(IGuildsFactory guildsFactory)
    {
        _guildsFactory = guildsFactory;
    }

    public async Task<IGuildItem?> GetGuildAsync(ulong snowflakeId) =>
        _guilds.TryGetValue(snowflakeId, out var guild) ? guild : null;

    public async Task<IGuildItem> LoadOrCreateGuildAsync(ulong snowflakeId)
    {
        var guild = _guildsFactory.Create();
        await guild.LoadOrCreateStateAsync(snowflakeId);

        _guilds.TryAdd(snowflakeId, guild);

        return guild;
    }

    public async Task DeleteAsync(ulong snowflakeId)
    {
        _guilds.TryRemove(snowflakeId, out var guild);
        if (guild is null)
            return;
        
        await guild.DeleteStateAsync();
    }

    // Only for testing
    internal bool AddGuild(ulong id, IGuildItem guild) =>
        _guilds.TryAdd(id, guild);
}