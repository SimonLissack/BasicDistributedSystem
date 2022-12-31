using System.Diagnostics;

namespace Infrastructure.Telemetry;

public static class TelemetryConstants
{
    public static string AppSource = "BasicDistributedSystem";
    public static readonly ActivitySource ActivitySource = new(AppSource);
}
