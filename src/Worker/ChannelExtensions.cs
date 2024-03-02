using Domain.Shared;
using Infrastructure.Messaging.RabbitMq;
using Infrastructure.Telemetry;
using RabbitMQ.Client;

namespace Worker;

public static class ChannelExtensions
{
    public static void ReplyToMessage<T>(this IModel channel, RabbitMqOptions options, string queueName, T message)
    {
        using var activity = TelemetryConstants.ActivitySource.StartActivity();

        var properties = channel.CreateJsonBasicProperties<T>();
        properties.InjectPropagationContext(activity);

        channel.BasicPublish(options.ExchangeName, queueName, properties, message.SerializeToMessage());
    }
}
