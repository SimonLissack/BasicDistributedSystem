using Domain.Shared;
using Domain.Shared.Models;
using Infrastructure.Messaging.RabbitMq;
using RabbitMQ.Client;

namespace WebClient.Services;

public interface IWorkRequestPublisherService
{
    void PublishWorkRequest(Guid id, int delayInSeconds);
}

public class WorkRequestPublisherService : IWorkRequestPublisherService
{
    readonly ILogger<WorkRequestPublisherService> _logger;
    readonly IRabbitMqChannelFactory _channelFactory;
    readonly RabbitMqConfiguration _configuration;

    public WorkRequestPublisherService(ILogger<WorkRequestPublisherService> logger, IRabbitMqChannelFactory channelFactory, RabbitMqConfiguration configuration)
    {
        _logger = logger;
        _channelFactory = channelFactory;
        _configuration = configuration;
    }

    public void PublishWorkRequest(Guid id, int delayInSeconds)
    {
        var channel = _channelFactory.GetChannel();
        var properties = channel.CreateJsonBasicProperties<RequestWork>();

        var body = new RequestWork
        {
            Id = id,
            DelayInSeconds = delayInSeconds
        }.SerializeToMessage();

        properties.ReplyTo = _channelFactory.ResponseQueueName;

        channel.BasicPublish(
            _configuration.WorkQueueName,
            nameof(RequestWork),
            properties,
            body
        );
    }
}
