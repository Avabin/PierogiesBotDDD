using Shared.Core.Events;

namespace Shared.Core.Queries;

public abstract record Query : Event, IQuery;