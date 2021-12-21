using System.Net.Mime;
using System.Text.Json;
using Domain.Shared.Models;
using Infrastructure.Messaging.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebClient.Models;

namespace WebClient.Services;

public class WorkQueueConsumerService : IHostedService
{
    readonly ILogger<WorkQueueConsumerService> _logger;
    readonly IRabbitMqChannelFactory _channelFactory;
    readonly IPingRepository _pingRepository;
    EventingBasicConsumer? _consumer;

    public WorkQueueConsumerService(ILogger<WorkQueueConsumerService> logger, IRabbitMqChannelFactory channelFactory, IPingRepository pingRepository)
    {
        _logger = logger;
        _channelFactory = channelFactory;
        _pingRepository = pingRepository;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var channel = _channelFactory.GetChannel();

        _consumer = new EventingBasicConsumer(channel);

        _consumer.Received += (_, ea) =>
        {
            _logger.LogInformation(
                "Received message from response queue {ResponseQueueName} Content type: {ContentType} Type: {Type}",
                _channelFactory.ResponseQueueName,
                ea.BasicProperties.ContentType,
                ea.BasicProperties.Type
            );

            if (ea.BasicProperties.ContentType == MediaTypeNames.Application.Json)
            {
                switch (ea.BasicProperties.Type)
                {
                    case nameof(AcceptedResponse):
                        var acceptedResponse = DeserializeMessage<AcceptedResponse>(ea.Body.ToArray());
                        UpdateModel(acceptedResponse.Id, m => m.AcceptedByWorkerAt = acceptedResponse.AcceptedAt);
                        break;
                    case nameof(ProcessingCompletedResponse):
                        var processingCompletedResponse = DeserializeMessage<ProcessingCompletedResponse>(ea.Body.ToArray());
                        UpdateModel(processingCompletedResponse.Id, m =>
                        {
                            m.CompletedByWorkerAt = processingCompletedResponse.CompletedAt;
                            m.CompletedAt = DateTime.UtcNow;
                        });
                        break;
                }
            }
        };

        _channelFactory.GetChannel().BasicConsume(
            _channelFactory.ResponseQueueName,
            consumer: _consumer,
            autoAck: true
        );

        return Task.CompletedTask;

        T DeserializeMessage<T>(byte[] body) => JsonSerializer.Deserialize<T>(body)!;
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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleaning up queue {ResponseQueueName}", _channelFactory.ResponseQueueName);

        _channelFactory.GetChannel().QueueDelete(_channelFactory.ResponseQueueName, false, false);

        return Task.CompletedTask;
    }
}
