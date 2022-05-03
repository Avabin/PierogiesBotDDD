using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.Core.Persistence;
using Shared.Core.SeedWork;

[assembly:InternalsVisibleTo("Shared.Mongo.Tests")]
namespace Shared.Mongo.MongoRepository;

internal class MongoRepositoryFactory : IMongoRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MongoRepositoryFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public IRepository<T> Create<T>(string collectionName) where T : Entity
    {
        var mongoClient  = _serviceProvider.GetRequiredService<IMongoClient>();
        var mongoOptions = _serviceProvider.GetRequiredService<IOptions<MongoSettings>>();
        var logger = _serviceProvider.GetRequiredService<ILogger<MongoRepository<T>>>();
        return new MongoRepository<T>(mongoClient, mongoOptions, collectionName, logger);
    }
}