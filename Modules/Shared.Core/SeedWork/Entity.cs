using System.Collections.Immutable;
using Shared.Core.Events;
using Shared.Core.MessageBroker;

namespace Shared.Core.SeedWork;

public abstract record Entity(ImmutableList<IDelivery<IEvent>> DomainEvents, string Id = "")
{
    public string Id   { get; set; } = Id;
    public string Type => GetType().Name;
}