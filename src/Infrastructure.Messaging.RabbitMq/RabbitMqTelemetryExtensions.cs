using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace Infrastructure.Messaging.RabbitMq;

public static class RabbitMqTelemetryExtensions
{
    public static void InjectPropagationValues(this IBasicProperties properties, Activity? activity)
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
}
