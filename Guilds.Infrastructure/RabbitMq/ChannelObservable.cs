using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Core.Events;
using Shared.Core.MessageBroker;

namespace Guilds.Infrastructure.RabbitMq;

public class ChannelObservable<T> : ChannelObservable, IObservable<Delivery> where T : IEvent
{
    private readonly string                            _queueName;
    private readonly ILogger                           _logger;

    public ChannelObservable(IModel                             channel, string queueName, ILogger logger,
                             Func<BasicDeliverEventArgs, bool>? filter   = null) : base(channel)
    {
        _queueName = queueName;
        _logger    = logger;

        var filteredEvents = Observable
                            .FromEventPattern<BasicDeliverEventArgs>(this, nameof(Received))
                            .Select(x => x.EventArgs)
                            .Where(filter ?? ((_) => true));

        ReceivedObservable = filteredEvents.Select(Transform);
    }

    protected IObservable<Delivery> ReceivedObservable { get; }

    private Delivery Transform(BasicDeliverEventArgs message)
    {
        var   props = message.BasicProperties;
        Guid? correlationId = null;
        if (Guid.TryParse(props.CorrelationId, out var id)) correlationId = id;
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(props.Timestamp.UnixTime);
        var replyTo = props.ReplyTo;

        var @event = message.Body.ToArray().ToEvent<T>();
        _logger.LogTrace("Received message with Id {Id}: {@Event}", correlationId, @event);

        return Delivery.Of(@event, correlationId, timestamp, replyTo);
    }

    public IDisposable Subscribe(IObserver<Delivery> observer)
    {
        Model.BasicConsume(this, _queueName, autoAck: true);
        var sub = ReceivedObservable.Subscribe(observer);

        return sub;
    }

    public override void Dispose()
    {
        Model.Dispose();
    }
}

public abstract class ChannelObservable : EventingBasicConsumer, IDisposable
{
    public virtual void Dispose()
    {
    }

    protected ChannelObservable(IModel model) : base(model)
    {
    }
}