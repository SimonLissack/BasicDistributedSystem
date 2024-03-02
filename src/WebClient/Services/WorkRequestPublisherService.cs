using Domain.Shared;
using Domain.Shared.Models;
using Infrastructure.Abstractions;
using Infrastructure.Messaging.RabbitMq;
using Infrastructure.Telemetry;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace WebClient.Services;

public interface IWorkRequestPublisherService
{
    Task<string?> PublishWorkRequest(Guid id, int delayInSeconds);
}

public class WorkRequestPublisherService : IWorkRequestPublisherService
{
    readonly ILogger<WorkRequestPublisherService> _logger;
    readonly Instrumentation _instrumentation;
    readonly IRabbitMqChannelFactory _channelFactory;
    readonly ISerializationService _serializationService;
    readonly RabbitMqOptions _options;
    readonly WebClientOptions _webClientOptions;

    public WorkRequestPublisherService(ILogger<WorkRequestPublisherService> logger, Instrumentation instrumentation, IRabbitMqChannelFactory channelFactory, IOptions<RabbitMqOptions> configuration, IOptions<WebClientOptions> webClientConfiguration, ISerializationService serializationService)
    {
        _logger = logger;
        _instrumentation = instrumentation;
        _channelFactory = channelFactory;
        _serializationService = serializationService;
        _options = configuration.Value;
        _webClientOptions = webClientConfiguration.Value;
    }

    public async Task<string?> PublishWorkRequest(Guid id, int delayInSeconds)
    {
        using var activity = _instrumentation.ActivitySource.StartActivity($"{_options.WorkQueueName} send");
        activity?.AddTag("ping.id", id);

        _logger.LogInformation("Publishing work request for {Id}", id);
        var channel = await _channelFactory.GetChannel();
        var properties = channel.CreateJsonBasicProperties<RequestWork>();

        properties.InjectPropagationContext(activity);

        var body = _serializationService.SerializeToMessage(new RequestWork
        {
            Id = id,
            DelayInSeconds = delayInSeconds
        });

        properties.ReplyTo = _webClientOptions.ResponseQueueName;

        channel.BasicPublish(
            _options.ExchangeName,
            nameof(RequestWork),
            properties,
            body
        );

        return activity?.TraceId.ToString();
    }
}
