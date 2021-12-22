using System.Net.Mime;
using RabbitMQ.Client;

namespace Infrastructure.Messaging.RabbitMq;

public static class ChannelExtensions
{
    public static IBasicProperties CreateJsonBasicProperties<TMessageType>(this IModel channel)
    {
        var properties = channel.CreateBasicProperties();
        properties.Type = typeof(TMessageType).Name;
        properties.ContentType = MediaTypeNames.Application.Json;

        return properties;
    }

    public static void QueueBind(this IModel channel, string queueName, string exchangeName, params string[] routingKeys)
    {
        foreach (var workRequestRoutingKey in routingKeys)
        {
            IModelExensions.QueueBind(channel, queueName, exchangeName, workRequestRoutingKey);
        }
    }
}
