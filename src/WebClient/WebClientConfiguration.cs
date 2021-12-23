namespace WebClient;

public class WebClientConfiguration
{
    public string ResponseQueueName { get; set; } = $"bds_response.{Guid.NewGuid():N}";
}
