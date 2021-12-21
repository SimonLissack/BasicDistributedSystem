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
    readonly WebClientConfiguration _webClientConfiguration;

    public WorkRequestPublisherService(ILogger<WorkRequestPublisherService> logger, IRabbitMqChannelFactory channelFactory, RabbitMqConfiguration configuration, WebClientConfiguration webClientConfiguration)
    {
        _logger = logger;
        _channelFactory = channelFactory;
        _configuration = configuration;
        _webClientConfiguration = webClientConfiguration;
    }

    public void PublishWorkRequest(Guid id, int delayInSeconds)
    {
        _logger.LogInformation("Publishing work request for {Id}", id);
        var channel = _channelFactory.GetChannel();
        var properties = channel.CreateJsonBasicProperties<RequestWork>();

        var body = new RequestWork
        {
            Id = id,
            DelayInSeconds = delayInSeconds
        }.SerializeToMessage();

        properties.ReplyTo = _webClientConfiguration.ResponseQueueName;

        channel.BasicPublish(
            _configuration.ExchangeName,
            nameof(RequestWork),
            properties,
            body
        );
    }
}
