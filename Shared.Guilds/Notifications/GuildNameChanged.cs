using Shared.Core.Notifications;

namespace Shared.Guilds.Notifications;

public record GuildNameChanged(string Name) : Notification
{
}