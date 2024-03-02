using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    readonly RabbitMqOptions _options;
    IModel? _channel;

    public RabbitMqChannelSingletonFactory(ILogger<RabbitMqChannelSingletonFactory> logger, IOptions<RabbitMqOptions> configuration)
    {
        _logger = logger;
        _options = configuration.Value;
    }

    public async Task<IModel> GetChannel()
    {
        if (_channel is { })
        {
            return _channel;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.PortNumber,
            DispatchConsumersAsync = true,
            UserName = _options.UserName ?? ConnectionFactory.DefaultPass,
            Password = _options.Password ?? ConnectionFactory.DefaultPass
        };

        var connection = await ConnectAndRetryOnFailure(factory);
        _channel = connection.CreateModel();

        _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Direct);
        // Always guarantee the work queue exists
        _channel.QueueDeclare(_options.WorkQueueName, exclusive: false, autoDelete: false);

        _channel.QueueBind(_options.WorkQueueName, _options.ExchangeName, RoutingKeys.WorkRequestRoutingKeys);

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
