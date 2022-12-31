using System.Diagnostics;

namespace Infrastructure.Telemetry;

public static class TelemetryConstants
{
    public const string AppSource = "BasicDistributedSystem";
    public static readonly ActivitySource ActivitySource = new(AppSource);
}
