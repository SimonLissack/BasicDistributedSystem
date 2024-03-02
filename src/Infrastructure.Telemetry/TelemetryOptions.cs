namespace Infrastructure.Telemetry;

public class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string ActivitySourceName { get; set; } = null!;
}
