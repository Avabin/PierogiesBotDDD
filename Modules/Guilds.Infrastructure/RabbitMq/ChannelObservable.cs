using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Core.Events;
using Shared.Core.MessageBroker;

namespace Guilds.Infrastructure.RabbitMq;

public class ChannelObservable<T> : ChannelObservable, IObservable<Delivery> where T : IEvent
{
    public ChannelObservable(IModel                             channel,
                             Func<BasicDeliverEventArgs, bool>? filter = null) : base(channel)
    {
        var filteredEvents = Observable
                            .FromEventPattern<BasicDeliverEventArgs>(this, nameof(Received))
                            .Select(x => x.EventArgs)
                            .Where(filter ?? ((_) => true));

        ReceivedObservable = filteredEvents.Select(Transform);
    }

    protected IObservable<Delivery> ReceivedObservable { get; }

    private static Delivery Transform(BasicDeliverEventArgs message)
    {
        var   props = message.BasicProperties;
        Guid? correlationId = null;
        if (Guid.TryParse(props.CorrelationId, out var id)) correlationId = id;
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(props.Timestamp.UnixTime);
        var replyTo = props.ReplyTo;

        var @event = message.Body.ToArray().ToEvent<T>();

        return Delivery.Of(@event, correlationId, timestamp, replyTo);
    }

    public IDisposable Subscribe(IObserver<Delivery> observer)
    {
        var sub = ReceivedObservable.Subscribe(observer);

        return sub;
    }
}

public abstract class ChannelObservable : EventingBasicConsumer, IDisposable
{
    protected ChannelObservable(IModel model) : base(model)
    {
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            base.Model.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}