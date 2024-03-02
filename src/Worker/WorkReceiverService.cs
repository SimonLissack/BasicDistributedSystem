using System.Diagnostics;
using System.Net.Mime;
using Domain.Shared;
using Domain.Shared.Models;
using Infrastructure.Abstractions;
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
    readonly Instrumentation _instrumentation;
    readonly RabbitMqOptions _rabbitMqOptions;
    readonly IRabbitMqChannelFactory _rabbitMqChannelFactory;
    readonly ISerializationService _serializationService;

    public WorkReceiverService(ILogger<WorkReceiverService> logger, Instrumentation instrumentation, IOptions<RabbitMqOptions> rabbitMqConfiguration, IRabbitMqChannelFactory rabbitMqChannelFactory, ISerializationService serializationService)
    {
        _logger = logger;
        _instrumentation = instrumentation;
        _rabbitMqOptions = rabbitMqConfiguration.Value;
        _rabbitMqChannelFactory = rabbitMqChannelFactory;
        _serializationService = serializationService;
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

            using var activity = _instrumentation.ActivitySource.StartActivity($"{nameof(WorkReceiverService)} receive", ActivityKind.Consumer, parentContext);
            activity?.AddMessagingTags(_rabbitMqOptions, ea.BasicProperties.ReplyTo);

            _logger.LogInformation("Message received");
            if (ea.BasicProperties is not { ContentType: MediaTypeNames.Application.Json, Type: nameof(RequestWork) } && string.IsNullOrEmpty(ea.BasicProperties.ReplyTo))
            {
                _logger.LogInformation("Could not process message");
                Ack();
                return;
            }

            var body = _serializationService.DeserializeMessage<RequestWork>(ea.Body.ToArray());
            _logger.LogInformation("Processing message for {BodyId}", body.Id);

            activity?.AddEvent(new ActivityEvent("Sending accepted response"));
            ReplyToMessage(channel, ea.BasicProperties.ReplyTo, new AcceptedResponse
            {
                Id = body.Id,
                AcceptedAt = DateTime.UtcNow
            });

            activity?.AddEvent(new ActivityEvent("Starting work"));
            await DoWork(body.DelayInSeconds);
            activity?.AddEvent(new ActivityEvent("Work complete"));

            ReplyToMessage(channel, ea.BasicProperties.ReplyTo, new ProcessingCompletedResponse
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
        using var activity = _instrumentation.ActivitySource.StartActivity($"{nameof(WorkReceiverService)} working");

        var delayInMilliseconds = delayInSeconds >= 0 ? delayInSeconds * 1000 : 0;

        _logger.LogInformation("Waiting for {DelayInMilliseconds}ms", delayInMilliseconds);
        await Task.Delay(delayInMilliseconds);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {ServiceName}", nameof(WorkReceiverService));
        return Task.CompletedTask;
    }

    void ReplyToMessage<T>(IModel channel, string queueName, T message)
    {
        using var activity = _instrumentation.ActivitySource.StartActivity();

        var properties = channel.CreateJsonBasicProperties<T>();
        properties.InjectPropagationContext(activity);

        channel.BasicPublish(_rabbitMqOptions.ExchangeName, queueName, properties, _serializationService.SerializeToMessage(message));
    }
}
