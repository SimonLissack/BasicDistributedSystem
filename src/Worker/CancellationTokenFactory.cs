using Microsoft.Extensions.Logging;

namespace Worker;

public class ConsoleCancellationTokenSourceFactory
{
    readonly ILogger<ConsoleCancellationTokenSourceFactory> _logger;

    public ConsoleCancellationTokenSourceFactory(ILogger<ConsoleCancellationTokenSourceFactory> logger)
    {
        _logger = logger;
    }

    public CancellationTokenSource Create()
    {
        var cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, _) =>
        {
            _logger.LogCritical("Cancellation key press detected, stopping");
            cancellationTokenSource.Cancel();
        };

        return cancellationTokenSource;
    }
}
