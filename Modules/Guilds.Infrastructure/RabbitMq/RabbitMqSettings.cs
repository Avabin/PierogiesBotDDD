namespace Guilds.Infrastructure.RabbitMq;

public record RabbitMqSettings
{
    public string Host               { get; set; } = "localhost";
    public int    Port               { get; set; } = 5672;
    public bool   Enabled            { get; set; }
    public string UserName           { get; set; } = "guest";
    public string Password           { get; set; } = "guest";
    public string ClientName         { get; set; } = "client";
    public string NotificationsTopic { get; set; } = "notifications";
}