using System.Net.Mime;
using Domain.Shared;
using Domain.Shared.Models;
using Infrastructure.Messaging.RabbitMq;
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

    public WorkResponseConsumerHostedService(ILogger<WorkResponseConsumerHostedService> logger, IRabbitMqChannelFactory channelFactory, IPingRepository pingRepository, WebClientConfiguration webClientConfiguration, RabbitMqConfiguration rabbitMqConfiguration)
    {
        _logger = logger;
        _channelFactory = channelFactory;
        _pingRepository = pingRepository;
        _webClientConfiguration = webClientConfiguration;
        _rabbitMqConfiguration = rabbitMqConfiguration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
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
        if (_pingRepository.TryGetModel(id, out var pingModel))
        {
            update(pingModel!);
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
