using Shared.Core.Commands;
using Shared.Core.Events;
using Shared.Core.Notifications;

namespace Shared.Core.MessageBroker;

public interface IMessageBroker
{
    public static string RpcQueueName => "rpc_queue";

    ValueTask<TResult> SendAndReceiveAsync<TRequest, TResult>(TRequest request)
        where TResult : IEvent where TRequest : IEvent;

    IObservable<Delivery> GetNotificationsObservable<T>(string routingKey                 = "") where T : INotification;
    ValueTask             NotifyAsync<T>(T                     message, string routingKey = "") where T : INotification;

    ValueTask SendToTopicAsync<T>(T message, string topic, string routingKey = "*") where T : IEvent;

    IObservable<Delivery> GetObservableForTopic<T>(string topic, string routingKey = "*") where T : IEvent;

    ValueTask SendToQueueAsync<T>(T message, string queueName, Guid? correlationId = null) where T : IEvent;

    IObservable<Delivery> GetObservableForQueue<T>(string queueName) where T : IEvent;

    public ValueTask SendCommandAsync<T>(T command, string? rpcQueueName = null) where T : ICommand;
}