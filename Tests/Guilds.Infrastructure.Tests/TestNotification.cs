using Shared.Core.Notifications;

namespace Guilds.Infrastructure.Tests;

public record TestNotification(string Message) : INotification
{
}