namespace WebClient;

public class WebClientOptions
{
    public const string SectionName = nameof(WebClientOptions);

    public string ResponseQueueName { get; set; } = $"bds_response.{Guid.NewGuid():N}";
}
