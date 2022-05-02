using Guilds.Domain.Aggregates.GuildAggregate;
using Microsoft.Extensions.DependencyInjection;

namespace Guilds.Mongo;

internal class GuildsFactory : IGuildsFactory
{
    private readonly IServiceProvider _serviceProvider;

    public GuildsFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public IGuildItem Create() => 
        _serviceProvider.GetRequiredService<IGuildItem>();
}