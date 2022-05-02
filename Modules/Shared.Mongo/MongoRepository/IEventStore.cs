using Shared.Core.Events;
using Shared.Core.MessageBroker;

namespace Shared.Mongo.MongoRepository;

public interface IEventStore
{
    IQueryable<IDelivery<IEvent>> Query();

    Task AddAsync(IDelivery<IEvent> delivery);
}