using Shared.Core.Events;

namespace Shared.Core.Notifications;

public record Notification : Event, INotification
{
}