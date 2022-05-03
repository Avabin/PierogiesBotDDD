using Shared.Core.Events;
using Shared.Core.MessageBroker;

namespace Shared.Mongo.MongoRepository;

public interface IEventStore
{
    IQueryable<Delivery> Query();

    Task AddAsync(Delivery delivery);
}