using System.Reactive.Linq;
using Guilds.Api.Extensions;
using Guilds.Domain.Aggregates.GuildAggregate;
using Shared.Core.Events;
using Shared.Core.Queries;
using Shared.Guilds.Queries;

namespace Guilds.Api.Queries;

public class QuerySubscribedChannelsHandler : QueryHandler<QuerySubscribedChannels>
{
    private readonly IGuildsAggregate _guildsAggregate;

    public QuerySubscribedChannelsHandler(IGuildsAggregate guildsAggregate)
    {
        _guildsAggregate = guildsAggregate;
    }
    protected override async Task<IEvent> HandleAsync(QuerySubscribedChannels query)
    {
        var guild = await _guildsAggregate.GetGuildAsync(query.GuildId);

        if (guild == null) return QuerySubscribedChannelsResult.Empty;

        var channels = await guild.StateObservable
                                  .Select(x => x.SubscribedChannels.Select(y => y.ToView()))
                                  .Take(1);
        
        return QuerySubscribedChannelsResult.Of(channels);
        
    }
}