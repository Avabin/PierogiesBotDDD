namespace Guilds.Domain.Aggregates.GuildAggregate;

public interface IGuildsAggregate
{
    Task<IGuildItem?> GetGuildAsync(ulong          snowflakeId);
    Task<IGuildItem>  LoadOrCreateGuildAsync(ulong snowflakeId);
    Task              DeleteAsync(ulong            snowflakeId);
}