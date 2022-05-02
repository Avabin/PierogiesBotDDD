namespace Shared.Core.Commands;

public interface ICommandHandler<in TCommand> : ICommandHandler where TCommand : ICommand
{
    Task HandleAsync(TCommand command);
}

public abstract class CommandHandler<TCommand> : ICommandHandler<TCommand> where TCommand : ICommand
{
    public abstract Task HandleAsync(TCommand command);

    public async Task HandleAsync(ICommand command) => await HandleAsync((TCommand)command);
}

public interface ICommandHandler
{
    Task HandleAsync(ICommand command);
}