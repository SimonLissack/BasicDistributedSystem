using System.Diagnostics;
using System.Net.Mime;
using Domain.Shared;
using Domain.Shared.Models;
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
    readonly IRabbitMqChannelFactory _channelFactory;
    readonly IPingRepository _pingRepository;
    readonly WebClientConfiguration _webClientConfiguration;
    readonly RabbitMqConfiguration _rabbitMqConfiguration;

    public WorkResponseConsumerHostedService(ILogger<WorkResponseConsumerHostedService> logger, IRabbitMqChannelFactory channelFactory, IPingRepository pingRepository, IOptions<WebClientConfiguration> webClientConfiguration, IOptions<RabbitMqConfiguration> rabbitMqConfiguration)
    {
        _logger = logger;
        _channelFactory = channelFactory;
        _pingRepository = pingRepository;
        _webClientConfiguration = webClientConfiguration.Value;
        _rabbitMqConfiguration = rabbitMqConfiguration.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[RabbitMQ] Host: {HostName}:{PortNumber}", _rabbitMqConfiguration.HostName, _rabbitMqConfiguration.PortNumber);
        _logger.LogInformation("[RabbitMQ] Exchange name: {ExchangeName}", _rabbitMqConfiguration.ExchangeName);
        _logger.LogInformation("[RabbitMQ] Work queue name: {WorkQueueName}", _rabbitMqConfiguration.WorkQueueName);

        var channel = await _channelFactory.GetChannel();

        channel.QueueDeclare(
            _webClientConfiguration.ResponseQueueName,
            false,
            false
        );

        channel.QueueBind(_webClientConfiguration.ResponseQueueName, _rabbitMqConfiguration.ExchangeName, _webClientConfiguration.ResponseQueueName);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += (_, ea) =>
        {
            var parentContext = ea.BasicProperties.ExtractPropagationContext();

            using var activity = TelemetryConstants.ActivitySource.StartActivity($"{nameof(WorkResponseConsumerHostedService)} receive", ActivityKind.Consumer, parentContext);

            _logger.LogInformation(
                "Received message from response queue {ResponseQueueName} Content type: {ContentType} Type: {Type}",
                _webClientConfiguration.ResponseQueueName,
                ea.BasicProperties.ContentType,
                ea.BasicProperties.Type
            );

            if (ea.BasicProperties.ContentType == MediaTypeNames.Application.Json)
            {
                switch (ea.BasicProperties.Type)
                {
                    case nameof(AcceptedResponse):
                        var acceptedResponse = ea.Body.ToArray().DeserializeMessage<AcceptedResponse>();
                        UpdateModel(acceptedResponse.Id, m => m.AcceptedByWorkerAt = acceptedResponse.AcceptedAt);
                        break;
                    case nameof(ProcessingCompletedResponse):
                        var processingCompletedResponse = ea.Body.ToArray().DeserializeMessage<ProcessingCompletedResponse>();
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
            _webClientConfiguration.ResponseQueueName,
            consumer: consumer,
            autoAck: true
        );
    }

    void UpdateModel(Guid id, Action<PingModel> update)
    {
        using var activity = TelemetryConstants.ActivitySource.StartActivity();

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
        _logger.LogInformation("Cleaning up queue {ResponseQueueName}", _webClientConfiguration.ResponseQueueName);

        var channel = await _channelFactory.GetChannel();

        channel.QueueDelete(_webClientConfiguration.ResponseQueueName, false, false);
    }
}
