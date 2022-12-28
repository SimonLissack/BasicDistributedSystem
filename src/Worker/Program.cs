using Infrastructure.Messaging.RabbitMq;
using Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using Worker;

var otelResourceBuilder = ResourceBuilder.CreateDefault()
    .AddEnvironmentVariableDetector();

var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.ConfigureHostConfiguration(c => c
    .SetBasePath(Environment.CurrentDirectory)
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", true)
);

hostBuilder.ConfigureServices((hostContext, serviceCollection) =>
{
    var rabbitMqConfig = new RabbitMqConfiguration();
    hostContext.Configuration.GetSection(nameof(RabbitMqConfiguration)).Bind(rabbitMqConfig);

    serviceCollection
        .AddSingleton(rabbitMqConfig)
        .InstallRabbitMqInfrastructure()
        .AddHostedService<WorkReceiverService>()
        .AddTransient<ConsoleCancellationTokenSourceFactory>();
});

hostBuilder.ConfigureLogging(builder => builder
    .ClearProviders()
    .AddOpenTelemetryLogging(otelResourceBuilder)
);

var host = hostBuilder.Build();

var cancellationToken = host.Services.GetService<ConsoleCancellationTokenSourceFactory>()!.Create().Token;

await host.RunAsync(cancellationToken);
