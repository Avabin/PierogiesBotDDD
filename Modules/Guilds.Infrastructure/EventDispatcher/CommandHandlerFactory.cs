using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Commands;

namespace Guilds.Infrastructure.EventDispatcher;

internal class CommandHandlerFactory : ICommandHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CommandHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICommandHandler GetHandler<TCommand>() where TCommand : ICommand =>
        _serviceProvider.GetRequiredService<ICommandHandler>();

    public ICommandHandler GetHandler(Type commandType)
    {
        var handlerType = typeof(CommandHandler<>).MakeGenericType(commandType);
        return (ICommandHandler)_serviceProvider.GetRequiredService(handlerType);
    }
}