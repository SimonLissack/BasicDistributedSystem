using System.Text;
using System.Text.Json;
using Infrastructure.Abstractions;
using Infrastructure.Telemetry;

namespace Infrastructure.Services;

public class SerializationService : ISerializationService
{
    readonly Instrumentation _instrumentation;

    public SerializationService(Instrumentation instrumentation)
    {
        _instrumentation = instrumentation;
    }

    public T DeserializeMessage<T>(byte[] body)
    {
        using var activity = _instrumentation.ActivitySource.StartActivity();
        return JsonSerializer.Deserialize<T>(body)!;
    }

    public byte[] SerializeToMessage<T>(T message)
    {
        using var activity = _instrumentation.ActivitySource.StartActivity();

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
    }
}
