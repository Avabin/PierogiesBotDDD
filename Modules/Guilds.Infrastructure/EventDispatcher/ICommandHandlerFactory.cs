using Shared.Core.Commands;

namespace Guilds.Infrastructure.EventDispatcher;

public interface ICommandHandlerFactory
{
    ICommandHandler GetHandler<TCommand>() where TCommand : ICommand;
    ICommandHandler           GetHandler(Type commandType);
}