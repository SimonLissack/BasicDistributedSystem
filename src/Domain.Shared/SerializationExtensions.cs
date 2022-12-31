using System.Text;
using System.Text.Json;
using Infrastructure.Telemetry;

namespace Domain.Shared;

public static class SerializationExtensions
{
    public static T DeserializeMessage<T>(this byte[] body)
    {
        using var activity = TelemetryConstants.ActivitySource.StartActivity();
        return JsonSerializer.Deserialize<T>(body)!;
    }

    public static byte[] SerializeToMessage<T>(this T message)
    {
        using var activity = TelemetryConstants.ActivitySource.StartActivity();

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
    }
}
