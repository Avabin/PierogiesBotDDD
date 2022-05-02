using System.Diagnostics.Contracts;
using System.Reactive.Linq;

namespace Shared.Core.Extensions;

public static class ObservableExtensions
{
    [Pure]
    public static IObservable<T> WhereNotNull<T>(this IObservable<T?> source) where T : class => 
        source.Where(x => x is not null)!;
}