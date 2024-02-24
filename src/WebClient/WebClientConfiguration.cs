namespace WebClient;

public class WebClientConfiguration
{
    public const string SectionName = nameof(WebClientConfiguration);

    public string ResponseQueueName { get; set; } = $"bds_response.{Guid.NewGuid():N}";
}
