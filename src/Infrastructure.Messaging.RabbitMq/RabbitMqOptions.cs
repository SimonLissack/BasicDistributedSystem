namespace Infrastructure.Messaging.RabbitMq;

public class RabbitMqOptions
{
    public const string SectionName = nameof(RabbitMqOptions);

    public string HostName { get; set; } = null!;
    public int PortNumber { get; set; } = 5672; // RabbitMQ default
    public string WorkQueueName { get; set; } = null!;
    public string ExchangeName { get; set; } = null!;
    public string? Password { get; set; } = null;
    public string? UserName { get; set; } = null;
}
