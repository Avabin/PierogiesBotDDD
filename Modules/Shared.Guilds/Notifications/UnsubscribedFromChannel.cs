using Shared.Core.Notifications;

namespace Shared.Guilds.Notifications;

public record UnsubscribedFromChannel(ulong ChannelId) : Notification
{
}