using RabbitMQ.Client;

namespace Infrastructure.Messaging.RabbitMq;

public interface IRabbitMqChannelFactory
{
    IModel GetChannel();
}

public class RabbitMqChannelSingletonFactory : IRabbitMqChannelFactory
{
    readonly RabbitMqConfiguration _configuration;
    IModel? _channel;

    public RabbitMqChannelSingletonFactory(RabbitMqConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IModel GetChannel()
    {
        if (_channel is { })
        {
            return _channel;
        }

        var factory = new ConnectionFactory { HostName = _configuration.HostName, Port = _configuration.PortNumber};

        _channel = factory.CreateConnection().CreateModel();

        _channel.QueueDeclare(_configuration.WorkQueueName, exclusive: false, autoDelete: false);

        return _channel;
    }
}
