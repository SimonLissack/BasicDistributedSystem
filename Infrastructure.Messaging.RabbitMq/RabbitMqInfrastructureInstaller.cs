using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Messaging.RabbitMq;

public static class RabbitMqInfrastructureInstaller
{
    public static IServiceCollection InstallRabbitMqInfrastructure(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection
            .AddSingleton(configuration.Get<RabbitMqConfiguration>())
            .AddSingleton<IRabbitMqChannelFactory, RabbitMqChannelSingletonFactory>();

        return serviceCollection;
    }
}
