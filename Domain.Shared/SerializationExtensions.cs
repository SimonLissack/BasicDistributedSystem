using System.Text;
using System.Text.Json;

namespace Domain.Shared;

public static class SerializationExtensions
{
    public static T DeserializeMessage<T>(this byte[] body) => JsonSerializer.Deserialize<T>(body)!;

    public static byte[] SerializeToMessage<T>(this T message) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
}
