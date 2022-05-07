using Shared.Core.Events;

namespace Shared.Core.Queries;

public interface IQueryHandler<in TQuery> : IQueryHandler where TQuery : IQuery
{
}

public interface IQueryHandler
{
    Task<IEvent> HandleAsync(IQuery query);
}

public abstract class QueryHandler<TQuery> : IQueryHandler<TQuery> where TQuery : IQuery
{
    protected abstract Task<IEvent> HandleAsync(TQuery query);

    public async Task<IEvent> HandleAsync(IQuery query) => await HandleAsync((TQuery)query);
}