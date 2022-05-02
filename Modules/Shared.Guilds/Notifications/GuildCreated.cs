using Shared.Core.Notifications;

namespace Shared.Guilds.Notifications;

public record GuildCreated(string Name, ulong SnowflakeId) : Notification
{
    
}