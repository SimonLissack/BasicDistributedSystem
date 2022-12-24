using System.Net.Mime;
using Domain.Shared;
using Domain.Shared.Models;
using Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Worker;

public class WorkReceiverService
{
    readonly ILogger<WorkReceiverService> _logger;
    readonly RabbitMqConfiguration _rabbitMqConfiguration;
    readonly IRabbitMqChannelFactory _rabbitMqChannelFactory;
    readonly ConsoleCancellationTokenSourceFactory _cancellationTokenSourceFactory;

    public WorkReceiverService(ILogger<WorkReceiverService> logger, RabbitMqConfiguration rabbitMqConfiguration, IRabbitMqChannelFactory rabbitMqChannelFactory, ConsoleCancellationTokenSourceFactory cancellationTokenSourceFactory)
    {
        _logger = logger;
        _rabbitMqConfiguration = rabbitMqConfiguration;
        _rabbitMqChannelFactory = rabbitMqChannelFactory;
        _cancellationTokenSourceFactory = cancellationTokenSourceFactory;
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting worker");
        _logger.LogInformation("Host:\t{HostName}:{PortNumber}", _rabbitMqConfiguration.HostName, _rabbitMqConfiguration.PortNumber);
        _logger.LogInformation("Queue:\t{WorkQueueName}", _rabbitMqConfiguration.WorkQueueName);
        _logger.LogInformation("Exchange:\t{ExchangeName}", _rabbitMqConfiguration.ExchangeName);

        var cancellationToken = _cancellationTokenSourceFactory.Create();

        var channel = await _rabbitMqChannelFactory.GetChannel();
        var consumer = CreateConsumer(channel);

        channel.BasicQos(0, 1, false);
        channel.BasicConsume(_rabbitMqConfiguration.WorkQueueName, false, consumer);

        await cancellationToken.WaitUntilCancelled();
    }

    AsyncEventingBasicConsumer CreateConsumer(IModel channel)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += async (_, ea) =>
        {
            _logger.LogInformation("Message received");
            if (ea.BasicProperties is not { ContentType: MediaTypeNames.Application.Json, Type: nameof(RequestWork) } && string.IsNullOrEmpty(ea.BasicProperties.ReplyTo))
            {
                _logger.LogInformation("Could not process message");
                Ack();
                return;
            }

            var body = ea.Body.ToArray().DeserializeMessage<RequestWork>();
            _logger.LogInformation("Processing message for {BodyId}", body.Id);
            channel.ReplyToMessage(_rabbitMqConfiguration, ea.BasicProperties.ReplyTo, new AcceptedResponse
            {
                Id = body.Id,
                AcceptedAt = DateTime.UtcNow
            });

            var delayInMilliseconds = body.DelayInSeconds >= 0 ? body.DelayInSeconds * 1000 : 0;

            _logger.LogInformation("Waiting for {DelayInMilliseconds}ms", delayInMilliseconds);
            await Task.Delay(delayInMilliseconds);

            channel.ReplyToMessage(_rabbitMqConfiguration, ea.BasicProperties.ReplyTo, new ProcessingCompletedResponse
            {
                Id = body.Id,
                CompletedAt = DateTime.UtcNow
            });

            Ack();
            _logger.LogInformation("Completed processing message for {BodyId}", body.Id);

            void Ack() => channel.BasicAck(ea.DeliveryTag, false);
        };

        return consumer;
    }
}
