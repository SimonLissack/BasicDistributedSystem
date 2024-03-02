namespace Infrastructure.Abstractions;

public interface ISerializationService
{
    T DeserializeMessage<T>(byte[] body);
    byte[] SerializeToMessage<T>(T message);
}
