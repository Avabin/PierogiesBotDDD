using Shared.Core.Notifications;

namespace Shared.Guilds.Notifications;

public record SubscribedToChannel(string Name, ulong ChannelId) : Notification
{
}