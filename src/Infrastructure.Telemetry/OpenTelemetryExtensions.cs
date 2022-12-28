using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace Infrastructure.Telemetry;

public static class OpenTelemetryExtensions
{
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.AddOpenTelemetry(o => o.AddConsoleExporter());
        return loggingBuilder;
    }
}
