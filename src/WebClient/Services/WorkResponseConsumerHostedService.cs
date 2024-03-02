using System.Diagnostics;
using System.Net.Mime;
using Domain.Shared;
using Domain.Shared.Models;
using Infrastructure.Abstractions;
using Infrastructure.Messaging.RabbitMq;
using Infrastructure.Telemetry;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebClient.Models;

namespace WebClient.Services;

public class WorkResponseConsumerHostedService : IHostedService
{
    readonly ILogger<WorkResponseConsumerHostedService> _logger;
    readonly Instrumentation _instrumentation;
    readonly IRabbitMqChannelFactory _channelFactory;
    readonly IPingRepository _pingRepository;
    readonly ISerializationService _serializationService;
    readonly WebClientOptions _webClientOptions;
    readonly RabbitMqOptions _rabbitMqOptions;

    public WorkResponseConsumerHostedService(
        ILogger<WorkResponseConsumerHostedService> logger,
        Instrumentation instrumentation,
        IRabbitMqChannelFactory channelFactory,
        IPingRepository pingRepository,
        IOptions<WebClientOptions> webClientConfiguration,
        IOptions<RabbitMqOptions> rabbitMqConfiguration,
        ISerializationService serializationService
        )
    {
        _logger = logger;
        _instrumentation = instrumentation;
        _channelFactory = channelFactory;
        _pingRepository = pingRepository;
        _serializationService = serializationService;
        _webClientOptions = webClientConfiguration.Value;
        _rabbitMqOptions = rabbitMqConfiguration.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[RabbitMQ] Host: {HostName}:{PortNumber}", _rabbitMqOptions.HostName, _rabbitMqOptions.PortNumber);
        _logger.LogInformation("[RabbitMQ] Exchange name: {ExchangeName}", _rabbitMqOptions.ExchangeName);
        _logger.LogInformation("[RabbitMQ] Work queue name: {WorkQueueName}", _rabbitMqOptions.WorkQueueName);

        var channel = await _channelFactory.GetChannel();

        channel.QueueDeclare(
            _webClientOptions.ResponseQueueName,
            false,
            false
        );

        channel.QueueBind(_webClientOptions.ResponseQueueName, _rabbitMqOptions.ExchangeName, _webClientOptions.ResponseQueueName);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += (_, ea) =>
        {
            var parentContext = ea.BasicProperties.ExtractPropagationContext();

            using var activity = _instrumentation.ActivitySource.StartActivity($"{nameof(WorkResponseConsumerHostedService)} receive", ActivityKind.Consumer, parentContext);

            _logger.LogInformation(
                "Received message from response queue {ResponseQueueName} Content type: {ContentType} Type: {Type}",
                _webClientOptions.ResponseQueueName,
                ea.BasicProperties.ContentType,
                ea.BasicProperties.Type
            );

            if (ea.BasicProperties.ContentType == MediaTypeNames.Application.Json)
            {
                switch (ea.BasicProperties.Type)
                {
                    case nameof(AcceptedResponse):
                        var acceptedResponse = _serializationService.DeserializeMessage<AcceptedResponse>(ea.Body.ToArray());
                        UpdateModel(acceptedResponse.Id, m => m.AcceptedByWorkerAt = acceptedResponse.AcceptedAt);
                        break;
                    case nameof(ProcessingCompletedResponse):
                        var processingCompletedResponse = _serializationService.DeserializeMessage<ProcessingCompletedResponse>(ea.Body.ToArray());
                        UpdateModel(processingCompletedResponse.Id, m =>
                        {
                            m.CompletedByWorkerAt = processingCompletedResponse.CompletedAt;
                            m.CompletedAt = DateTime.UtcNow;
                        });
                        break;
                    default:
                        _logger.LogWarning("Unknown message type: {Type}", ea.BasicProperties.Type);
                        break;
                }
            }
            else
            {
                _logger.LogWarning("Unknown content type: {ContentType}", ea.BasicProperties.ContentType);
            }

            return Task.CompletedTask;
        };

        channel.BasicConsume(
            _webClientOptions.ResponseQueueName,
            consumer: consumer,
            autoAck: true
        );
    }

    void UpdateModel(Guid id, Action<PingModel> update)
    {
        using var activity = _instrumentation.ActivitySource.StartActivity();

        if (_pingRepository.TryGetModel(id, out var pingModel))
        {
            update(pingModel!);
            _pingRepository.SaveModel(pingModel!);
            return;
        }
        _logger.LogWarning("Could not find model with id {Id}", id);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleaning up queue {ResponseQueueName}", _webClientOptions.ResponseQueueName);

        var channel = await _channelFactory.GetChannel();

        channel.QueueDelete(_webClientOptions.ResponseQueueName, false, false);
    }
}
