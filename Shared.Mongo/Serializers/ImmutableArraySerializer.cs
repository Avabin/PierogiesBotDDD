using System.Collections.Immutable;
using MongoDB.Bson.Serialization;

namespace Shared.Mongo.Serializers;

public class ImmutableListSerializer<T> : ValueEnumerableSerializerBase<ImmutableList<T>, T>, IChildSerializerConfigurable
{
    public ImmutableListSerializer()
    {
    }

    public ImmutableListSerializer(IBsonSerializer<T> itemSerializer)
        : base(itemSerializer)
    {
    }

    public ImmutableListSerializer(IBsonSerializerRegistry serializerRegistry)
        : base(serializerRegistry)
    {
    }

    public IBsonSerializer WithItemSerializer(IBsonSerializer<T> itemSerializer)
    {
        return new ImmutableListSerializer<T>(itemSerializer);
    }

    protected override void AddItem(object accumulator, T item)
    {
        ((ImmutableList<T>.Builder)accumulator).Add(item);
    }

    protected override object CreateAccumulator()
    {
        return ImmutableList.CreateBuilder<T>();
    }

    protected override IEnumerable<T> EnumerateItemsInSerializationOrder(ImmutableList<T> value)
    {
        return value;
    }

    protected override ImmutableList<T> FinalizeResult(object accumulator)
    {
        return ((ImmutableList<T>.Builder)accumulator).ToImmutable();
    }

    IBsonSerializer IChildSerializerConfigurable.ChildSerializer => ItemSerializer;

    IBsonSerializer IChildSerializerConfigurable.WithChildSerializer(IBsonSerializer childSerializer)
    {
        return new ImmutableListSerializer<T>((IBsonSerializer<T>)childSerializer);
    }
}