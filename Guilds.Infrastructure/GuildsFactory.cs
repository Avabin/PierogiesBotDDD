using Guilds.Domain.Aggregates.GuildAggregate;
using Microsoft.Extensions.DependencyInjection;

namespace Guilds.Infrastructure;

internal class GuildsFactory : IGuildsFactory
{
    private readonly IServiceProvider _serviceProvider;

    public GuildsFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public GuildItem Create() => 
        _serviceProvider.GetRequiredService<GuildItem>();
}