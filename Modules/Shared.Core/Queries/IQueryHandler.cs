using Shared.Core.Events;

namespace Shared.Core.Queries;

public interface IQueryHandler<in TQuery> : IQueryHandler where TQuery : IQuery
{
    Task<IEvent>                  HandleAsync(TQuery query);
    public new async Task<IEvent> HandleAsync(IQuery query) => await HandleAsync((TQuery)query);
}

public interface IQueryHandler
{
    Task<IEvent> HandleAsync(IQuery query);
}

public abstract class QueryHandler<TQuery> : IQueryHandler<TQuery> where TQuery : IQuery
{
    public abstract Task<IEvent> HandleAsync(TQuery query);

    public async Task<IEvent> HandleAsync(IQuery query) => await HandleAsync((TQuery)query);
}