using System.Collections.Concurrent;
using Shared.Core.SeedWork;

namespace Guilds.Domain.Aggregates.GuildAggregate;

public class GuildsAggregate : IAggregateRoot, IGuildsAggregate
{
    private readonly IGuildsFactory                     _guildsFactory;
    private readonly ConcurrentDictionary<ulong, GuildItem> _guilds = new();

    public GuildsAggregate(IGuildsFactory guildsFactory)
    {
        _guildsFactory = guildsFactory;
    }
    public async Task<GuildItem> GetGuildAsync(ulong snowflakeId)
    {
        if (_guilds.TryGetValue(snowflakeId, out var guild))
        {
            return guild;
        }

        guild =_guildsFactory.Create();
        await guild.LoadStateAsync(snowflakeId);

        _guilds.TryAdd(snowflakeId, guild);
        
        return guild;
    }
}