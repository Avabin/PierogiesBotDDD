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

    public IQueryHandler GetHandler<TQuery>() where TQuery : IQuery =>
        _serviceProvider.GetRequiredService<IQueryHandler>();

    public IQueryHandler GetHandler(Type queryType) => 
        (IQueryHandler) _serviceProvider.GetRequiredService(typeof(IQueryHandler).MakeGenericType(queryType));
}