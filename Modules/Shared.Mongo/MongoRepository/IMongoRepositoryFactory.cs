using Shared.Core.Persistence;
using Shared.Core.SeedWork;

namespace Shared.Mongo.MongoRepository;

public interface IMongoRepositoryFactory
{
    IRepository<T> Create<T>(string collectionName) where T : Entity;
}