namespace Guilds.Infrastructure.RabbitMq;

public class InvalidResponseTypeException : Exception
{
    public InvalidResponseTypeException(string msg) : base(msg)
    {
    }
}