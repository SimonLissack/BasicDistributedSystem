namespace Infrastructure.Messaging.RabbitMq;

public class RabbitMqConfiguration
{
    public string HostName { get; set; } = null!;
    public int PortNumber { get; set; } = 5672; // RabbitMQ default
    public string WorkQueueName { get; set; } = null!;
    public string ExchangeName { get; set; } = null!;
}
