using System.Diagnostics;
using System.Net.Mime;
using Domain.Shared;
using Domain.Shared.Models;
using Infrastructure.Messaging.RabbitMq;
using Infrastructure.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Worker;

public class WorkReceiverService : IHostedService
{
    readonly ILogger<WorkReceiverService> _logger;
    readonly RabbitMqOptions _rabbitMqOptions;
    readonly IRabbitMqChannelFactory _rabbitMqChannelFactory;

    public WorkReceiverService(ILogger<WorkReceiverService> logger, IOptions<RabbitMqOptions> rabbitMqConfiguration, IRabbitMqChannelFactory rabbitMqChannelFactory)
    {
        _logger = logger;
        _rabbitMqOptions = rabbitMqConfiguration.Value;
        _rabbitMqChannelFactory = rabbitMqChannelFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting worker");
        _logger.LogInformation("Host: {HostName}:{PortNumber}", _rabbitMqOptions.HostName, _rabbitMqOptions.PortNumber);
        _logger.LogInformation("Queue: {WorkQueueName}", _rabbitMqOptions.WorkQueueName);
        _logger.LogInformation("Exchange: {ExchangeName}", _rabbitMqOptions.ExchangeName);

        var channel = await _rabbitMqChannelFactory.GetChannel();
        var consumer = CreateConsumer(channel);

        channel.BasicQos(0, 1, false);
        channel.BasicConsume(_rabbitMqOptions.WorkQueueName, false, consumer);
    }

    AsyncEventingBasicConsumer CreateConsumer(IModel channel)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += async (_, ea) =>
        {
            var parentContext = ea.BasicProperties.ExtractPropagationContext();

            using var activity = TelemetryConstants.ActivitySource.StartActivity($"{nameof(WorkReceiverService)} receive", ActivityKind.Consumer, parentContext);
            activity?.AddMessagingTags(_rabbitMqOptions, ea.BasicProperties.ReplyTo);

            _logger.LogInformation("Message received");
            if (ea.BasicProperties is not { ContentType: MediaTypeNames.Application.Json, Type: nameof(RequestWork) } && string.IsNullOrEmpty(ea.BasicProperties.ReplyTo))
            {
                _logger.LogInformation("Could not process message");
                Ack();
                return;
            }

            var body = ea.Body.ToArray().DeserializeMessage<RequestWork>();
            _logger.LogInformation("Processing message for {BodyId}", body.Id);
            channel.ReplyToMessage(_rabbitMqOptions, ea.BasicProperties.ReplyTo, new AcceptedResponse
            {
                Id = body.Id,
                AcceptedAt = DateTime.UtcNow
            });

            await DoWork(body.DelayInSeconds);

            channel.ReplyToMessage(_rabbitMqOptions, ea.BasicProperties.ReplyTo, new ProcessingCompletedResponse
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

    async Task DoWork(int delayInSeconds)
    {
        using var activity = TelemetryConstants.ActivitySource.StartActivity($"{nameof(WorkReceiverService)} working");

        var delayInMilliseconds = delayInSeconds >= 0 ? delayInSeconds * 1000 : 0;

        _logger.LogInformation("Waiting for {DelayInMilliseconds}ms", delayInMilliseconds);
        await Task.Delay(delayInMilliseconds);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {ServiceName}", nameof(WorkReceiverService));
        return Task.CompletedTask;
    }
}
