using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Infrastructure.Messaging.RabbitMq;

public interface IRabbitMqChannelFactory
{
    Task<IModel> GetChannel();
}

public class RabbitMqChannelSingletonFactory : IRabbitMqChannelFactory
{
    readonly ILogger<RabbitMqChannelSingletonFactory> _logger;
    readonly RabbitMqConfiguration _configuration;
    IModel? _channel;

    public RabbitMqChannelSingletonFactory(ILogger<RabbitMqChannelSingletonFactory> logger, RabbitMqConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IModel> GetChannel()
    {
        if (_channel is { })
        {
            return _channel;
        }

        var factory = new ConnectionFactory
        {
            HostName = _configuration.HostName,
            Port = _configuration.PortNumber,
            DispatchConsumersAsync = true
        };

        var connection = await ConnectAndRetryOnFailure(factory);
        _channel = connection.CreateModel();

        _channel.ExchangeDeclare(_configuration.ExchangeName, ExchangeType.Direct);
        // Always guarantee the work queue exists
        _channel.QueueDeclare(_configuration.WorkQueueName, exclusive: false, autoDelete: false);

        _channel.QueueBind(_configuration.WorkQueueName, _configuration.ExchangeName, RoutingKeys.WorkRequestRoutingKeys);

        return _channel;
    }

    async Task<IConnection> ConnectAndRetryOnFailure(ConnectionFactory factory)
    {
        const int maxAttempts = 5;
        var exceptions = new List<Exception>();
        for (var retry = 0; retry < maxAttempts; retry++)
        {
            try
            {
                return factory.CreateConnection();
            }
            catch (BrokerUnreachableException e)
            {
                exceptions.Add(e);
                _logger.LogInformation("Attempt {Attempt} of {MaxAttempts} failed: {Message}", retry, maxAttempts, e.Message);
                await Task.Delay(5000);
            }
        }

        _logger.LogError("Failed to connect RabbitMQ");
        throw new AggregateException(exceptions);
    }
}
