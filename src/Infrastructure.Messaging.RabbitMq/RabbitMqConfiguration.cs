namespace Infrastructure.Messaging.RabbitMq;

public class RabbitMqConfiguration
{
    public const string SectionName = nameof(RabbitMqConfiguration);

    public string HostName { get; set; } = null!;
    public int PortNumber { get; set; } = 5672; // RabbitMQ default
    public string WorkQueueName { get; set; } = null!;
    public string ExchangeName { get; set; } = null!;
    public string? Password { get; set; } = null;
    public string? UserName { get; set; } = null;
}
