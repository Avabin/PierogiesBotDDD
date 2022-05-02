using Shared.Core.Events;

namespace Shared.Core.Commands;

public record Command : Event, ICommand
{
}