using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Core.Commands;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.Queries;

namespace Guilds.Infrastructure.EventDispatcher;

public class EventDispatcherHostedService: IHostedService
{
    private readonly CompositeDisposable    _d = new();
    private readonly IMessageBroker         _messageBroker;
    private readonly ICommandHandlerFactory _commandHandlerFactory;
    
    private readonly IQueryHandlerFactory                            _queryHandlerFactory;
    private readonly ILogger<EventDispatcherHostedService> _logger;

    public EventDispatcherHostedService(IMessageBroker messageBroker, ICommandHandlerFactory commandHandlerFactory, IQueryHandlerFactory queryHandlerFactory, ILogger<EventDispatcherHostedService> logger)
    {
        _messageBroker            = messageBroker;
        _commandHandlerFactory    = commandHandlerFactory;
        _queryHandlerFactory = queryHandlerFactory;
        _logger                   = logger;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EventDispatcherHostedService is starting");
        var rpcObservable = _messageBroker.GetObservableForQueue<IEvent>(IMessageBroker.RpcQueueName);
        _d.Add(rpcObservable
              .Select(x => (x.CorrelationId, data: x.Data as ICommand))
              .Where(x => x.data is not null)
              .Do(x => _logger.LogTrace("Handling command {CommandType}", x.data?.GetType().Name))
              .Select(x => Observable.FromAsync(() => HandleCommand(x.data!)))
              .Concat()
              .Subscribe());
        
        _d.Add(rpcObservable
              .Select(x => (x.CorrelationId, x.ReplyTo, data: x.Data as IQuery))
              .Where(x => x.data is not null)
              .Do(x => _logger.LogTrace("Handling query {QueryType}", x.data?.GetType().Name))
              .Select(x => Observable.FromAsync(() => HandleQuery(x.data!, x.CorrelationId, x.ReplyTo)))
              .Concat()
              .Subscribe());
        
        return Task.CompletedTask;
    }
    
    private async Task HandleQuery(IQuery query, Guid? correlationId, string replyTo)
    {
        var handler = _queryHandlerFactory.GetHandler(query.GetType());
        var result  = await handler.HandleAsync(query);
        _logger.LogTrace("Sending query result {CorrelationId} {ResultType} to {Target}", correlationId, result.GetType().Name, replyTo);
        await _messageBroker.SendToQueueAsync(result, replyTo, correlationId);
    }
    
    private async Task HandleCommand(ICommand command)
    {
        var handler = _commandHandlerFactory.GetHandler(command.GetType());
        await handler.HandleAsync(command);
    }

    public Task StopAsync(CancellationToken  cancellationToken)
    {
        _logger.LogInformation("EventDispatcherHostedService is stopping");
        _d.Dispose();

        return Task.CompletedTask;
    }
}