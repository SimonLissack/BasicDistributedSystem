using System.Diagnostics;
using System.Text;
using Infrastructure.Telemetry;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace Infrastructure.Messaging.RabbitMq;

public static class RabbitMqTelemetryExtensions
{
    public static void InjectPropagationContext(this IBasicProperties properties, Activity? activity)
    {

        ActivityContext contextToInject = default;
        if (activity != null)
        {
            contextToInject = activity.Context;
        }
        else if (Activity.Current != null)
        {
            contextToInject = Activity.Current.Context;
        }

        Propagators.DefaultTextMapPropagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, InjectTraceContext);
    }

    private static void InjectTraceContext(IBasicProperties properties, string key, string value)
    {
        properties.Headers ??= new Dictionary<string, object>();

        properties.Headers[key] = value;
    }

    public static ActivityContext ExtractPropagationContext(this IBasicProperties properties)
    {
        var parentContext = Propagators.DefaultTextMapPropagator.Extract(default, properties, ExtractTraceContext);
        Baggage.Current = parentContext.Baggage;

        return parentContext.ActivityContext;
    }

    static IEnumerable<string> ExtractTraceContext(IBasicProperties properties, string key) =>
        properties.Headers.TryGetValue(key, out var value)
            ? new[] { Encoding.UTF8.GetString((byte[])value) }
            : Enumerable.Empty<string>();

    public static void AddMessagingTags(this Activity? activity, RabbitMqConfiguration configuration, string destinationQueue)
    {
        // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
        // See:
        //   * https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#messaging-attributes
        //   * https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#rabbitmq
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination_kind", "queue");
        activity?.SetTag("messaging.destination", configuration.ExchangeName);
        activity?.SetTag("messaging.rabbitmq.routing_key", destinationQueue);
    }
}
