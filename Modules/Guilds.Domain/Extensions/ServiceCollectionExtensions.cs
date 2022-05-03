using Guilds.Domain.Aggregates.GuildAggregate;
using Microsoft.Extensions.DependencyInjection;

namespace Guilds.Domain.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGuilds(this IServiceCollection s) =>
        s.AddTransient<GuildItem>();
}