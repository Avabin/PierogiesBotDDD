﻿using Guilds.Domain.Aggregates.GuildAggregate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using Shared.Guilds.Commands;
using Shared.Mongo.Extensions;
using Shared.Mongo.Serializers;

namespace Guilds.Mongo;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGuildsMongo(this IServiceCollection services, IConfiguration config)
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
        services.AddTransient<IGuildsFactory, GuildsFactory>();
        services.AddSingleton<IGuildsAggregate, GuildsAggregate>();
        services.AddTransient<IGuildItem, GuildItem>();
        services.AddTransient<IGuildService, GuildService>();
        services.AddMongoDb(config.GetConnectionString("MongoDB"), "PierogiesBot");
        return services;
    }
}