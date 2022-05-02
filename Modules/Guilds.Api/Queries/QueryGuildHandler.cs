using System.Reactive.Linq;
using Guilds.Api.Extensions;
using Guilds.Domain.Aggregates.GuildAggregate;
using Shared.Core.Events;
using Shared.Core.Queries;
using Shared.Guilds.Queries;

namespace Guilds.Api.Queries;

public class QueryGuildHandler : QueryHandler<QueryGuild>
{
    private readonly IGuildsAggregate _guildsAggregate;

    public QueryGuildHandler(IGuildsAggregate guildsAggregate)
    {
        _guildsAggregate = guildsAggregate;
    }
    public override async Task<IEvent> HandleAsync(QueryGuild query)
    {
        var guild = await _guildsAggregate.GetGuildAsync(query.SnowflakeId);
        if (guild is null) return new QueryGuildResult(GuildState.Empty.ToView());

        var state = await guild.StateObservable.FirstAsync();
        return new QueryGuildResult(state.ToView());
    }
}