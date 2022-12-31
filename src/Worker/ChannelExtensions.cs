using Domain.Shared;
using Infrastructure.Messaging.RabbitMq;
using Infrastructure.Telemetry;
using RabbitMQ.Client;

public static class ChannelExtensions
{
    public static void ReplyToMessage<T>(this IModel channel, RabbitMqConfiguration configuration, string queueName, T message)
    {
        using var activity = TelemetryConstants.ActivitySource.StartActivity();

        var properties = channel.CreateJsonBasicProperties<T>();
        properties.InjectPropagationContext(activity);

        channel.BasicPublish(configuration.ExchangeName, queueName, properties, message.SerializeToMessage());
    }
}
