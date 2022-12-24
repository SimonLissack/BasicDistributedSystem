using System.Net.Mime;
using Domain.Shared;
using Domain.Shared.Models;
using Infrastructure.Messaging.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Worker;

public class WorkReceiverService
{
    readonly RabbitMqConfiguration _rabbitMqConfiguration;
    readonly IRabbitMqChannelFactory _rabbitMqChannelFactory;
    readonly ConsoleCancellationTokenSourceFactory _cancellationTokenSourceFactory;

    public WorkReceiverService(RabbitMqConfiguration rabbitMqConfiguration, IRabbitMqChannelFactory rabbitMqChannelFactory, ConsoleCancellationTokenSourceFactory cancellationTokenSourceFactory)
    {
        _rabbitMqConfiguration = rabbitMqConfiguration;
        _rabbitMqChannelFactory = rabbitMqChannelFactory;
        _cancellationTokenSourceFactory = cancellationTokenSourceFactory;
    }

    public async Task StartAsync()
    {
        Console.WriteLine($"Host:\t{_rabbitMqConfiguration.HostName}:{_rabbitMqConfiguration.PortNumber}");
        Console.WriteLine($"Queue:\t{_rabbitMqConfiguration.WorkQueueName}");
        Console.WriteLine($"Exchange:\t{_rabbitMqConfiguration.ExchangeName}");

        var cancellationToken = _cancellationTokenSourceFactory.Create();

        var channel = await _rabbitMqChannelFactory.GetChannel();

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
            channel.ReplyToMessage(_rabbitMqConfiguration, ea.BasicProperties.ReplyTo, new AcceptedResponse
            {
                Id = body.Id,
                AcceptedAt = DateTime.UtcNow
            });

            var delayInMilliseconds = body.DelayInSeconds >= 0 ? body.DelayInSeconds * 1000 : 0;

            Console.WriteLine($"[x] Waiting for {delayInMilliseconds}ms");
            await Task.Delay(delayInMilliseconds);

            channel.ReplyToMessage(_rabbitMqConfiguration, ea.BasicProperties.ReplyTo, new ProcessingCompletedResponse
            {
                Id = body.Id,
                CompletedAt = DateTime.UtcNow
            });

            Ack();
            Console.WriteLine($"[x] Completed processing message for {body.Id}");

            void Ack() => channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume(_rabbitMqConfiguration.WorkQueueName, false, consumer);

        await cancellationToken.WaitUntilCancelled();
    }
}
