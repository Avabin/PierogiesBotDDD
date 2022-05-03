using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Shared.Mongo.Serializers;

// https://stackoverflow.com/a/57829411
public abstract class ValueEnumerableSerializerBase<TValue, TItem> : SerializerBase<TValue>, IBsonArraySerializer
    where TValue : IEnumerable<TItem>
{
    private readonly Lazy<IBsonSerializer<TItem>> _lazyItemSerializer;

    protected ValueEnumerableSerializerBase()
        : this(BsonSerializer.SerializerRegistry)
    {
    }

    protected ValueEnumerableSerializerBase(IBsonSerializer<TItem> itemSerializer)
    {
        if (itemSerializer == null)
            throw new ArgumentNullException(nameof(itemSerializer));

        _lazyItemSerializer = new Lazy<IBsonSerializer<TItem>>(() => itemSerializer);
    }

    protected ValueEnumerableSerializerBase(IBsonSerializerRegistry serializerRegistry)
    {
        if (serializerRegistry == null)
            throw new ArgumentNullException(nameof(serializerRegistry));

        _lazyItemSerializer = new Lazy<IBsonSerializer<TItem>>(() => serializerRegistry.GetSerializer<TItem>());
    }

    public IBsonSerializer<TItem> ItemSerializer => _lazyItemSerializer.Value;

    public override TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        IBsonReader reader = context.Reader;

        BsonType currentBsonType = reader.GetCurrentBsonType();

        switch (currentBsonType)
        {
            case BsonType.Null:
            {
                reader.ReadNull();
                return default;
            }
            case BsonType.Array:
            {
                reader.ReadStartArray();
                object accumulator = CreateAccumulator();

                while (reader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    TItem item = _lazyItemSerializer.Value.Deserialize(context);
                    AddItem(accumulator, item);
                }

                reader.ReadEndArray();
                return FinalizeResult(accumulator);
            }
            default:
            {
                throw CreateCannotDeserializeFromBsonTypeException(currentBsonType);
            }
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        IBsonWriter writer = context.Writer;

        if (value.Equals(default))
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteStartArray();
            foreach (TItem item in EnumerateItemsInSerializationOrder(value))
                _lazyItemSerializer.Value.Serialize(context, item);
            writer.WriteEndArray();
        }
    }

    public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
    {
        IBsonSerializer<TItem> itemSerializer = _lazyItemSerializer.Value;
        serializationInfo = new BsonSerializationInfo(null, itemSerializer, itemSerializer.ValueType);
        return true;
    }

    protected abstract void AddItem(object accumulator, TItem item);

    protected abstract object CreateAccumulator();

    protected abstract IEnumerable<TItem> EnumerateItemsInSerializationOrder(TValue value);

    protected abstract TValue FinalizeResult(object accumulator);
}