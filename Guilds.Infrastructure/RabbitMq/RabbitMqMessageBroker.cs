using System.Collections.Concurrent;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Core.Events;
using Shared.Core.MessageBroker;
using Shared.Core.Notifications;

namespace Guilds.Infrastructure.RabbitMq;

public class RabbitMqMessageBroker : IMessageBroker, IDisposable
{
    // RabbitMQ connection settings
    private readonly IOptions<RabbitMqSettings> _options;
    private readonly ILoggerFactory             _loggerFactory;
    private readonly ILogger<RabbitMqMessageBroker> _logger;
    public           RabbitMqSettings           Options              => _options.Value;
    public          string                     RpcCallbackQueueName => $"{Options.ClientName}-callback";
    
    // RabbitMQ connection factory
    private readonly Lazy<ConnectionFactory> _connectionFactory;
    public           ConnectionFactory       ConnectionFactory => _connectionFactory.Value;
    
    // RabbitMQ connection
    private readonly Lazy<IConnection> _connection;
    public           IConnection         Connection => _connection.Value;

    private readonly ConcurrentDictionary<string, ChannelObservable> _observables = new();

    public RabbitMqMessageBroker(IOptions<RabbitMqSettings> options, ILoggerFactory loggerFactory)
    {
        _options            = options;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<RabbitMqMessageBroker>();

        _connectionFactory = new Lazy<ConnectionFactory>(() =>
        {
            var factory = new ConnectionFactory
            {
                HostName = Options.Host,
                Port = Options.Port,
                UserName = Options.UserName,
                Password = Options.Password
            };
            
            return factory;
        });
        
        _connection = new Lazy<IConnection>(() =>
        {
            var connection = ConnectionFactory.CreateConnection();
            return connection;
        });
    }
    public async ValueTask<T> SendAndReceiveAsync<T, TEvent>(TEvent request) where TEvent : IEvent where T : IEvent
    {
        var correlationId = Guid.NewGuid();
        var eventType = typeof(TEvent).Name;
        
        _logger.LogDebug("{ActionName}: Sending request of type {RequestType}", nameof(SendAndReceiveAsync), eventType);
        using var channel = Connection.CreateModel();
        
        _logger.LogTrace("{ActionName}: Declaring RPC queue", nameof(SendAndReceiveAsync));
        channel.QueueDeclare(queue: "rpc_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var props = channel.CreateProperties(correlationId: correlationId);
        props.ReplyTo = RpcCallbackQueueName;

        _logger.LogTrace("{ActionName}: Declaring callback queue {QueueName}", nameof(SendAndReceiveAsync), RpcCallbackQueueName);
        channel.QueueDeclare(queue: RpcCallbackQueueName, exclusive: false);
        var channelObservable = new ChannelObservable<T>(channel, RpcCallbackQueueName,
                                                         _loggerFactory.CreateLogger<ChannelObservable<T>>(), 
                                                         filter: d => d.BasicProperties.CorrelationId == props.CorrelationId);

        _logger.LogTrace("{ActionName}: Publishing request of type {RequestType}", nameof(SendAndReceiveAsync), eventType);
        channel.BasicPublish("", "rpc_queue", basicProperties: props, request.ToMessageBody());
        
        _logger.LogTrace("{ActionName}: Waiting for response of type {ResponseType} for Id {CorrelationId}", nameof(SendAndReceiveAsync), typeof(T).Name, props.CorrelationId);
        var response = await channelObservable.FirstAsync();
        
        _logger.LogTrace("{ActionName}: Received response of type {ResponseType} for Id {CorrelationId}", nameof(SendAndReceiveAsync), typeof(T).Name, props.CorrelationId);
        if (response.Data is T data) return data;
        throw new InvalidResponseTypeException($"{nameof(SendAndReceiveAsync)}: Expected response of type {typeof(T).Name} but received {response.Data.GetType().Name}");
    }
    
    

    public IObservable<Delivery> GetNotificationsObservable<T>(string routingKey = "") where T : INotification
    {
        if (_observables.TryGetValue(routingKey, out var notificationsObservable)) return notificationsObservable.AsOf<T>() ?? throw new InvalidOperationException($"{nameof(GetNotificationsObservable)}: No observable for type {typeof(T).Name}");

        var eventType = typeof(T).Name;
        
        _logger.LogDebug("{ActionName}: Creating observable for notifications of type {NotificationType}", nameof(GetNotificationsObservable), eventType);
        var channel = Connection.CreateModel();
        channel.ExchangeDeclare(exchange: Options.NotificationsTopic, type: ExchangeType.Direct);
        var clientQueue = channel.QueueDeclare().QueueName;
        
        _logger.LogTrace("{ActionName}: Binding queue {QueueName} to notifications topic {TopicName} with routing key {RoutingKey}", nameof(GetNotificationsObservable), clientQueue, Options.NotificationsTopic, routingKey);
        channel.QueueBind(queue: clientQueue, exchange: Options.NotificationsTopic, routingKey: routingKey);
        
        var channelObservable = new ChannelObservable<T>(channel, clientQueue,_loggerFactory.CreateLogger<ChannelObservable<T>>());
        
        _observables.TryAdd(routingKey, channelObservable);
        
        return channelObservable;
    }

    public async ValueTask NotifyAsync<T>(T message, string routingKey = "") where T : INotification
    {
        var eventType = typeof(T).Name;
        
        _logger.LogDebug("{ActionName}: Sending notification of type {NotificationType}", nameof(NotifyAsync), eventType);
        using var channel = Connection.CreateModel();
        
        _logger.LogTrace("{ActionName}: Declaring notifications topic", nameof(NotifyAsync));
        channel.ExchangeDeclare(exchange: Options.NotificationsTopic, type: ExchangeType.Direct);
        var props = channel.CreateProperties();
        
        _logger.LogTrace("{ActionName}: Publishing notification of type {NotificationType}", nameof(NotifyAsync), eventType);
        channel.BasicPublish(exchange: "notifications", routingKey: routingKey, basicProperties: props, body: message.ToMessageBody());
    }

    public ValueTask    SendToTopicAsync<T>(T                 message, string topic, string routingKey = "*") where T : IEvent
    {
        using var channel = Connection.CreateModel();
        channel.ExchangeDeclare(exchange: topic, type: ExchangeType.Topic);
        
        var body = message.ToMessageBody();
        var props = channel.CreateProperties();
        channel.BasicPublish(exchange: topic, routingKey: routingKey, basicProperties: props, body: body);
        
        return ValueTask.CompletedTask;
    }
    public ValueTask SendToQueueAsync<T>(T message, string queueName, Guid? correlationId = null) where T : IEvent
    {
        using var channel = Connection.CreateModel();
        var props = channel.CreateProperties(correlationId);
        channel.BasicPublish("", queueName, props, message.ToMessageBody());

        return ValueTask.CompletedTask;
    }
    

    public IObservable<Delivery> GetObservableForTopic<T>(string topic, string routingKey = "*") where T : IEvent
    {
        if (_observables.TryGetValue(routingKey, out var notificationsObservable)) return notificationsObservable.AsOf<T>() ?? throw new InvalidOperationException($"{nameof(GetNotificationsObservable)}: No observable for type {typeof(T).Name}");

        var channel = Connection.CreateModel();
        channel.ExchangeDeclare(exchange: topic, type: ExchangeType.Topic);
        var queueName = channel.QueueDeclare(exclusive: false).QueueName;
        channel.QueueBind(queue: queueName, exchange: topic, routingKey: routingKey);
        
        var channelObservable = new ChannelObservable<T>(channel, queueName, _loggerFactory.CreateLogger<ChannelObservable<T>>());
        
        _observables.TryAdd($"{topic}.{routingKey}", channelObservable);
        
        return channelObservable;
    }
    
    public IObservable<Delivery> GetObservableForQueue<T>(string queueName) where T : IEvent
    {
        if(_observables.TryGetValue(queueName, out var observable)) return observable.AsOf<T>() ?? throw new InvalidOperationException($"{nameof(GetObservableForQueue)}: No observable for type {typeof(T).Name}");
        
        var channel = Connection.CreateModel();
        channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        var channelObservable = new ChannelObservable<T>(channel, queueName, _loggerFactory.CreateLogger<ChannelObservable<T>>());
        
        
        _observables.TryAdd(queueName, channelObservable);
        
        return channelObservable;
    }

    public void Dispose()
    {
        // dispose connection if value is created
        if (_connection.IsValueCreated) _connection.Value.Dispose();

        foreach (var channelObservable in _observables.Values)
        {
            channelObservable.Dispose();
        }
    }
}