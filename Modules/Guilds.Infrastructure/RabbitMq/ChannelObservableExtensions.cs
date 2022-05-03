using Shared.Core.Events;

namespace Guilds.Infrastructure.RabbitMq;

public static class ChannelObservableExtensions
{
    public static ChannelObservable<T>? AsOf<T>(this ChannelObservable co) where T : IEvent =>
        co as ChannelObservable<T>;
}