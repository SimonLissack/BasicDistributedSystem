using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Infrastructure.Telemetry;

public static class OpenTelemetryInstallationExtensions
{
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.AddOpenTelemetry(o => o.AddConsoleExporter());
        return loggingBuilder;
    }

    public static IServiceCollection AddOpenTelemetryStack(this IServiceCollection services, IConfiguration configuration, string environmentName, Action<TracerProviderBuilder>? tracingConfiguration = null)
    {
        var telemetrySection = configuration.GetSection(TelemetryOptions.SectionName);
        var telemetryOptions = telemetrySection.Get<TelemetryOptions>() ?? new TelemetryOptions();

        services
            .Configure<TelemetryOptions>(telemetrySection)
            .AddSingleton<Instrumentation>()
            .AddOpenTelemetry()
            .ConfigureResource(r => r.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("host.name", Environment.MachineName),
                new KeyValuePair<string, object>("deployment.environment", environmentName)
            }))
            .WithTracing(b => b
                .AddZipkinExporter(c => c.Endpoint = telemetryOptions.ZipkinEndpoint)
                .AddSource(telemetryOptions.ActivitySourceName)
                .WithCustomTracing(tracingConfiguration)
            );

        return services;
    }

    static TracerProviderBuilder WithCustomTracing(this TracerProviderBuilder tracerProviderBuilder, Action<TracerProviderBuilder>? tracingConfiguration = null)
    {
        tracingConfiguration?.Invoke(tracerProviderBuilder);
        return tracerProviderBuilder;
    }
}
