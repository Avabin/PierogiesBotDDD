using System.Collections.Concurrent;
using Shared.Core.SeedWork;

namespace Guilds.Domain.Aggregates.GuildAggregate;

public class GuildsAggregate : IAggregateRoot, IGuildsAggregate
{
    private readonly IGuildsFactory                          _guildsFactory;
    private readonly ConcurrentDictionary<ulong, IGuildItem> _guilds = new();

    public GuildsAggregate(IGuildsFactory guildsFactory)
    {
        _guildsFactory = guildsFactory;
    }
    public async Task<IGuildItem?> GetGuildAsync(ulong snowflakeId) => _guilds.TryGetValue(snowflakeId, out var guild) ? guild : null;

    public async Task<IGuildItem> CreateGuildAsync(ulong snowflakeId)
    {
        var guild = _guildsFactory.Create();
        await guild.LoadStateAsync(snowflakeId);

        _guilds.TryAdd(snowflakeId, guild);
        
        return guild;
    }
}