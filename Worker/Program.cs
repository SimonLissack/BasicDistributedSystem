using System.Net.Mime;
using Domain.Shared;
using Domain.Shared.Models;
using Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var host = ConfigureHost();

Console.WriteLine("Starting worker role");

var rabbitMqConfiguration = host.Services.GetService<RabbitMqConfiguration>()!;

Console.WriteLine($"Host:\t{rabbitMqConfiguration.HostName}:{rabbitMqConfiguration.PortNumber}");
Console.WriteLine($"Queue:\t{rabbitMqConfiguration.WorkQueueName}");
Console.WriteLine($"Exchange:\t{rabbitMqConfiguration.ExchangeName}");

var channel = host.Services.GetService<IRabbitMqChannelFactory>()!.GetChannel();

channel.BasicQos(0, 1, false);

var consumer = new AsyncEventingBasicConsumer(channel);

consumer.Received += async (_, ea) =>
{
    Console.WriteLine($"[x] Message received");
    if (ea.BasicProperties is not { ContentType: MediaTypeNames.Application.Json, Type: nameof(RequestWork) } && string.IsNullOrEmpty(ea.BasicProperties.ReplyTo))
    {
        Console.WriteLine($"[x] Could not process message");
        Ack();
        return;
    }

    var body = ea.Body.ToArray().DeserializeMessage<RequestWork>();
    Console.WriteLine($"[x] Processing message for {body.Id}");
    channel.ReplyToMessage(rabbitMqConfiguration, ea.BasicProperties.ReplyTo, new AcceptedResponse
    {
        Id = body.Id,
        AcceptedAt = DateTime.UtcNow
    });

    var delayInMilliseconds = body.DelayInSeconds >= 0 ? body.DelayInSeconds * 1000 : 0;

    Console.WriteLine($"[x] Waiting for {delayInMilliseconds}ms");
    await Task.Delay(delayInMilliseconds);

    channel.ReplyToMessage(rabbitMqConfiguration, ea.BasicProperties.ReplyTo, new ProcessingCompletedResponse
    {
        Id = body.Id,
        CompletedAt = DateTime.UtcNow
    });

    Ack();
    Console.WriteLine($"[x] Completed processing message for {body.Id}");

    void Ack() => channel.BasicAck(ea.DeliveryTag, false);
};

channel.BasicConsume(rabbitMqConfiguration.WorkQueueName, false, consumer);

Console.WriteLine("Press [Enter] to exit");
Console.ReadLine();

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
                .InstallRabbitMqInfrastructure();
        }
    ).Build();

public static class ChannelExtensions
{
    public static void ReplyToMessage<T>(this IModel channel, RabbitMqConfiguration configuration, string queueName, T message)
    {
        var properties = channel.CreateJsonBasicProperties<T>();
        channel.BasicPublish(configuration.ExchangeName, queueName, properties, message.SerializeToMessage());
    }
}
