using Domain.Shared.Models;

namespace Infrastructure.Messaging.RabbitMq;

public static class RoutingKeys
{
    public static readonly string[] WorkRequestRoutingKeys =
    {
        nameof(RequestWork)
    };
}
