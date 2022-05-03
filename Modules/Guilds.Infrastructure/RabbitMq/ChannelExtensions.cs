using RabbitMQ.Client;

namespace Guilds.Infrastructure.RabbitMq;

public static class ChannelExtensions
{
    public static IBasicProperties CreateProperties(this IModel     channel, Guid? correlationId = null,
                                                    DateTimeOffset? timestamp = null)
    {
        correlationId ??= Guid.NewGuid();
        timestamp     ??= DateTimeOffset.UtcNow;
        var properties = channel.CreateBasicProperties();
        properties.CorrelationId = correlationId.ToString();
        properties.Timestamp     = new AmqpTimestamp(timestamp.Value.ToUnixTimeMilliseconds());
        return properties;
    }
}