using System.Net.Mime;
using RabbitMQ.Client;

namespace Infrastructure.Messaging.RabbitMq;

public static class ChannelExtensions
{
    public static IBasicProperties CreateJsonBasicProperties<TMessageType>(this IModel channel)
    {
        var properties = channel.CreateBasicProperties();
        properties.Type = nameof(TMessageType);
        properties.ContentType = MediaTypeNames.Application.Json;

        return properties;
    }
}
