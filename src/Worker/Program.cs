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

var host = Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(c => c
        .SetBasePath(Environment.CurrentDirectory)
        .AddEnvironmentVariables()
        .AddJsonFile("appsettings.json", true)
    ).ConfigureServices((hostContext, serviceCollection) =>
        {
            var rabbitMqConfig = new RabbitMqConfiguration();
            hostContext.Configuration.GetSection(nameof(RabbitMqConfiguration)).Bind(rabbitMqConfig);

            serviceCollection
                .AddSingleton(rabbitMqConfig)
                .InstallRabbitMqInfrastructure()
                .AddHostedService<WorkReceiverService>()
                .AddTransient<ConsoleCancellationTokenSourceFactory>();
        }
    )
    .ConfigureLogging(builder => builder
        .ClearProviders()
        .AddOpenTelemetryLogging(otelResourceBuilder)
    )
    .Build();

var cancellationToken = host.Services.GetService<ConsoleCancellationTokenSourceFactory>()!.Create().Token;

await host.RunAsync(cancellationToken);
