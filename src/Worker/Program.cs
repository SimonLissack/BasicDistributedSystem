using Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Worker;

var host = ConfigureHost();

Console.WriteLine("Starting worker role");

IHost ConfigureHost() => Host.CreateDefaultBuilder(args)
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
                .AddSingleton<WorkReceiverService>();
        }
    ).Build();

var cancellationToken = CancellationTokenExtensions.CreateConsoleCancellationToken();

var workReceiverService = host.Services.GetService<WorkReceiverService>()!;

await workReceiverService.StartAsync();

await cancellationToken.WaitUntilCancelled();
