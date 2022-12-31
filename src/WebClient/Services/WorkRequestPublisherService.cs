using Domain.Shared;
using Domain.Shared.Models;
using Infrastructure.Messaging.RabbitMq;
using Infrastructure.Telemetry;
using RabbitMQ.Client;

namespace WebClient.Services;

public interface IWorkRequestPublisherService
{
    Task PublishWorkRequest(Guid id, int delayInSeconds);
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

    public async Task PublishWorkRequest(Guid id, int delayInSeconds)
    {
        using var activity = TelemetryConstants.ActivitySource.StartActivity($"{_configuration.WorkQueueName} send");
        activity?.AddTag("ping.id", id);

        _logger.LogInformation("Publishing work request for {Id}", id);
        var channel = await _channelFactory.GetChannel();
        var properties = channel.CreateJsonBasicProperties<RequestWork>();

        properties.InjectPropagationValues(activity);

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
