using System.Reflection;
using Guilds.Api;
using Guilds.Api.Commands;
using Guilds.Api.Extensions;
using Guilds.Api.Queries;
using Guilds.Domain.Aggregates.GuildAggregate;
using Guilds.Infrastructure.EventDispatcher;
using Guilds.Infrastructure.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using Shared.Core.Commands;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.Queries;
using Shared.Guilds.Commands;
using Shared.Mongo;
using Shared.Mongo.Extensions;
using Shared.Mongo.Serializers;

namespace Guilds.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Commands
        BsonClassMap.RegisterClassMap<ChangeGuildName>();
        BsonClassMap.RegisterClassMap<SubscribeChannel>();
        BsonClassMap.RegisterClassMap<UnsubscribeChannel>();
        
        // Entities
        BsonClassMap.RegisterClassMap<GuildState>(cm =>
        {
            cm.AutoMap();
            cm.MapProperty(gs => gs.SubscribedChannels).SetSerializer(new ImmutableListSerializer<SubscribedChannel>());
            
        });
        services.AddEventHandlers(typeof(ViewMapExtensions).Assembly).AddHostedService<EventDispatcherHostedService>();
        services.Configure<RabbitMqSettings>(config.GetSection("RabbitMQ"));
        services.AddTransient<IGuildService, GuildService>();
        services.AddTransient<IGuildsFactory, GuildsFactory>();
        services.AddSingleton<IGuildsAggregate, GuildsAggregate>();
        services.AddTransient<IGuildItem, GuildItem>();
        services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();
        services.AddSingleton<ICommandHandlerFactory, CommandHandlerFactory>();
        services.AddSingleton<IQueryHandlerFactory, QueryHandlerFactory>();
        services.AddTransient<ICommandHandler<ChangeGuildName>, ChangeGuildNameHandler>();
        services.AddMongoDb(config.GetConnectionString("MongoDB"), "Guilds");
        return services;
    }
    
    // Add event handlers
    public static IServiceCollection AddEventHandlers(this IServiceCollection services, Assembly assembly) =>
        services.AddCommandHandlers(assembly)
                .AddQueryHandlers(assembly);

    public static IServiceCollection AddCommandHandlers(this IServiceCollection services, Assembly assembly)
    {
        var commandHandlers = assembly.GetExportedTypes().Where(t => t.IsClass && t.GetInterfaces()
                                                             .Any(i => i.IsGenericType &&
                                                                       i.GetGenericTypeDefinition() ==
                                                                       typeof(ICommandHandler<>)))
                .ToList();

        foreach (var commandHandler in commandHandlers)
        {
            var interfaceType = commandHandler.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
            services.AddTransient(interfaceType, commandHandler);
        }
        
        return services;
    }
    
    public static IServiceCollection AddQueryHandlers(this IServiceCollection services, Assembly assembly)
    {
        var queryHandlers = assembly.GetExportedTypes().Where(t => t.IsClass && t.GetInterfaces()
                                                             .Any(i => i.IsGenericType &&
                                                                       i.GetGenericTypeDefinition() ==
                                                                       typeof(IQueryHandler<>)))
                .ToList();

        foreach (var queryHandler in queryHandlers)
        {
            var interfaceType = queryHandler.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<>));
            services.AddTransient(interfaceType, queryHandler);
        }
        
        return services;
    }
}