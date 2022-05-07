using Shared.Core.Queries;

namespace Guilds.Infrastructure.EventDispatcher;

public interface IQueryHandlerFactory
{
    IQueryHandler GetHandler<TQuery>() where TQuery : IQuery;

    IQueryHandler GetHandler(Type queryType);
}