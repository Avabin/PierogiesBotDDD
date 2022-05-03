using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.SeedWork;
using Shared.Mongo.MongoRepository;
using Shared.Mongo.Serializers;

namespace Shared.Mongo.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureMongoBaseSerializers(this IServiceCollection _)
    {
        BsonClassMap.RegisterClassMap<Event>();
        BsonClassMap.RegisterClassMap<Delivery>(cm =>
        {
            cm.AutoMap();
            cm.MapIdProperty(x => x.Id).SetIdGenerator(StringObjectIdGenerator.Instance);
        });
        BsonClassMap.RegisterClassMap<Entity>(cm =>
        {
            cm.MapIdProperty(gs => gs.Id)
              .SetIdGenerator(new StringObjectIdGenerator());
            cm.MapProperty(gs => gs.DomainEvents).SetSerializer(new ImmutableListSerializer<IDelivery<IEvent>>());
        });
        return _;
    }

    public static IServiceCollection AddMongoDb(this IServiceCollection services, string connectionString,
                                                string                  databaseName)
    {
        services.ConfigureMongoBaseSerializers();
        services.AddOptions<MongoSettings>().Configure(x => x.Database = databaseName);
        services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
        services.AddTransient<IMongoRepositoryFactory, MongoRepositoryFactory>();
        services.AddSingleton<IEventStore, EventStore>();
        return services;
    }
}