using System.Runtime.Serialization;

namespace Guilds.Infrastructure.RabbitMq;

[Serializable]
public class InvalidResponseTypeException : Exception
{
    public InvalidResponseTypeException(string msg) : base(msg)
    {
    }
    
    // ISerializable pattern
    protected InvalidResponseTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
    
    // ReSharper disable once RedundantOverriddenMember
    public override void GetObjectData(SerializationInfo info, StreamingContext context) => base.GetObjectData(info, context);
}