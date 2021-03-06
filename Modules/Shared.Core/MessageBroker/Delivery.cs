using Shared.Core.Events;

namespace Shared.Core.MessageBroker;

public interface IDelivery<out T>
{
    string          Id       { get; set; }
    T               Data          { get; }
    Guid?           CorrelationId { get; }
    DateTimeOffset? Timestamp     { get; }
}

public record Delivery(IEvent Data, Guid? CorrelationId, DateTimeOffset? Timestamp, string ReplyTo) : IDelivery<IEvent>
{
    public static Delivery Of<T>(T       data,           Guid? correlationId = null, DateTimeOffset? timestamp = null,
                                 string? replyTo = null, string id = "") where T : IEvent
    {
        correlationId ??= Guid.NewGuid();
        timestamp     ??= DateTimeOffset.UtcNow;
        return new Delivery(data, correlationId, timestamp, replyTo ?? "") { Id = id };
    }

    public string Id { get; set; } = "";
}