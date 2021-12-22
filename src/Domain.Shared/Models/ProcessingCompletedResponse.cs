namespace Domain.Shared.Models;

public class ProcessingCompletedResponse
{
    public Guid Id { get; set; }
    public DateTime CompletedAt { get; set; }
}
