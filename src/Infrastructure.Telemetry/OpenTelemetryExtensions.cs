using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Infrastructure.Telemetry;

public static class OpenTelemetryExtensions
{
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.AddOpenTelemetry(o => o.AddConsoleExporter());
        return loggingBuilder;
    }

    public static IServiceCollection AddOpenTelemetryStack(this IServiceCollection services, string environmentName, Action<TracerProviderBuilder>? tracingConfiguration = null)
    {
        services
            .AddOpenTelemetry()
            .ConfigureResource(r => r.AddAttributes(new []
            {
                new KeyValuePair<string, object>("host.name", Environment.MachineName),
                new KeyValuePair<string, object>("deployment.environment", environmentName)
            }))
            .WithTracing(b => b
                .AddJaegerExporter()
                .AddSource(TelemetryConstants.AppSource)
                .WithCustomTracing(tracingConfiguration)
            ).StartWithHost();

        return services;
    }

    static TracerProviderBuilder WithCustomTracing(this TracerProviderBuilder tracerProviderBuilder, Action<TracerProviderBuilder>? tracingConfiguration = null)
    {
        tracingConfiguration?.Invoke(tracerProviderBuilder);
        return tracerProviderBuilder;
    }
}
