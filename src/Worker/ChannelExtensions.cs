using Domain.Shared;
using Infrastructure.Messaging.RabbitMq;
using RabbitMQ.Client;

public static class ChannelExtensions
{
    public static void ReplyToMessage<T>(this IModel channel, RabbitMqConfiguration configuration, string queueName, T message)
    {
        var properties = channel.CreateJsonBasicProperties<T>();
        channel.BasicPublish(configuration.ExchangeName, queueName, properties, message.SerializeToMessage());
    }
}