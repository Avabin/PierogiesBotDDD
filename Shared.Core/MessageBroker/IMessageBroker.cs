using Shared.Core.Events;
using Shared.Core.Notifications;

namespace Shared.Core.MessageBroker;

public interface IMessageBroker
{
    public static string RpcQueueName => "rpc_queue";
    ValueTask<T>         SendAndReceiveAsync<T, TEvent>(TEvent request) where TEvent : IEvent where T : IEvent;

    IObservable<Delivery> GetNotificationsObservable<T>(string routingKey = "") where T : INotification;
    ValueTask NotifyAsync<T>(T message, string routingKey = "") where T : INotification;

    ValueTask SendToTopicAsync<T>(T message, string topic, string routingKey = "*") where T : IEvent;

    IObservable<Delivery> GetObservableForTopic<T>(string topic, string routingKey = "*") where T : IEvent;

    ValueTask SendToQueueAsync<T>(T message, string queueName, Guid? correlationId = null) where T : IEvent;

    IObservable<Delivery> GetObservableForQueue<T>(string queueName) where T : IEvent;
}