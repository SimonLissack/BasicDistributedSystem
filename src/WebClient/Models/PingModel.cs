namespace WebClient.Models;

public class PingModel
{
    public Guid Id { get; set; }
    public int DelayInSeconds { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? AcceptedByWorkerAt { get; set; }
    public DateTime? CompletedByWorkerAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
