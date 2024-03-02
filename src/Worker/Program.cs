using Infrastructure.Messaging.RabbitMq;
using Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Worker;

var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.ConfigureHostConfiguration(c => c
    .SetBasePath(Environment.CurrentDirectory)
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", true)
);

hostBuilder.ConfigureServices((hostContext, serviceCollection) =>
{
    serviceCollection
        .Configure<RabbitMqOptions>(hostContext.Configuration.GetSection(RabbitMqOptions.SectionName));

    serviceCollection
        .InstallRabbitMqInfrastructure()
        .AddHostedService<WorkReceiverService>()
        .AddTransient<ConsoleCancellationTokenSourceFactory>();

    serviceCollection.AddOpenTelemetryStack(hostContext.HostingEnvironment.EnvironmentName);
});

hostBuilder.ConfigureLogging(builder => builder
    .ClearProviders()
    .AddOpenTelemetryLogging()
);

var host = hostBuilder.Build();

var cancellationToken = host.Services.GetService<ConsoleCancellationTokenSourceFactory>()!.Create().Token;

await host.RunAsync(cancellationToken);
