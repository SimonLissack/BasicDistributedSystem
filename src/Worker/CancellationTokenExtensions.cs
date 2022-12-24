namespace Worker;

public static class CancellationTokenExtensions
{
    public static CancellationTokenSource CreateConsoleCancellationToken()
    {
        var cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, _) =>
        {
            Console.WriteLine("Cancellation key press detected, stopping.");
            cancellationTokenSource.Cancel();
        };

        return cancellationTokenSource;
    }

    // Cribbed from https://github.com/dotnet/runtime/issues/14991#issuecomment-131221355
    public static async Task WaitUntilCancelled(this CancellationTokenSource cancellationTokenSource)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        cancellationTokenSource.Token.Register(s => ((TaskCompletionSource<bool>)s!).SetResult(true), taskCompletionSource);

        await taskCompletionSource.Task;
    }
}
