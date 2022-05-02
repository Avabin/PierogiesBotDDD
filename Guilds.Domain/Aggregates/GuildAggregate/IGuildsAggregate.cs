namespace Guilds.Domain.Aggregates.GuildAggregate;

public interface IGuildsAggregate
{
    Task<GuildItem> GetGuildAsync(ulong snowflakeId);
}