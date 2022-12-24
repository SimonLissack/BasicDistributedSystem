using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace Infrastructure.Telemetry;

public static class OpenTelemetryExtensions
{
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder loggingBuilder, ResourceBuilder resourceBuilder)
    {
        loggingBuilder.AddOpenTelemetry(o => o.AddConsoleExporter().SetResourceBuilder(resourceBuilder));
        return loggingBuilder;
    }
}
