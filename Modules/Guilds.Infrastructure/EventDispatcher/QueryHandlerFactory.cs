using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Queries;

namespace Guilds.Infrastructure.EventDispatcher;

public class QueryHandlerFactory : IQueryHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public QueryHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IQueryHandler<TQuery> GetHandler<TQuery>() where TQuery : IQuery =>
        _serviceProvider.GetRequiredService<IQueryHandler<TQuery>>();

    public IQueryHandler GetHandler(Type queryType)
    {
        return (IQueryHandler)_serviceProvider.GetRequiredService(typeof(IQueryHandler<>).MakeGenericType(queryType));
    }
}