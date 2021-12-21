namespace WebClient;

public class WebClientConfiguration
{
    public string ResponseQueueName { get; set; } = $"bms_response.{Guid.NewGuid():N}";
}
