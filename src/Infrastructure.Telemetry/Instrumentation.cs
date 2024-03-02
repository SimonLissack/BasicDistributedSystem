using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Infrastructure.Telemetry;

public class Instrumentation
{
    public ActivitySource ActivitySource { get; }

    public Instrumentation(IOptions<TelemetryOptions> telemetryOptions)
    {
        var telemetryOptions1 = telemetryOptions.Value;
        ActivitySource = new ActivitySource(telemetryOptions1.ActivitySourceName);
    }
}
