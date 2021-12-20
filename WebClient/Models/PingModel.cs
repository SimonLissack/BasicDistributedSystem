namespace WebClient.Models;

public class PingModel
{
    public Guid Id { get; set; }
    public int DelayInSeconds { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReceivedByWorkerAt { get; set; }
    public DateTime? ProcessedByWorkerAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
