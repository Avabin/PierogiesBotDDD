using Shared.Core.Events;
using Shared.Guilds.Views;

namespace Shared.Guilds.Queries;

public record QueryGuildResult(GuildView Guild) : Event
{
    
}