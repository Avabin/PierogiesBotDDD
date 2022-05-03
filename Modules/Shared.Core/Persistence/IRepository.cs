using System.Linq.Expressions;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.SeedWork;

namespace Shared.Core.Persistence;

public interface IRepository<T> where T : Entity
{
    Task<T?> FindByIdAsync(string id);

    Task<T?> FindOneByFieldAsync<TField>(Expression<Func<T, TField>> field,
                                         TField                      value);

    Task<T> InsertAsync(T      entity);
    Task    UpdateAsync(T      entity);
    Task    DeleteAsync(string id);

    Task AddDomainEventAsync(IDelivery<IEvent>    @event, string id);
    Task RemoveDomainEventAsync(IDelivery<IEvent> @event, string id);
}