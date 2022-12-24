namespace Worker;

public static class CancellationTokenExtensions
{
    // Cribbed from https://github.com/dotnet/runtime/issues/14991#issuecomment-131221355
    public static async Task WaitUntilCancelled(this CancellationTokenSource cancellationTokenSource)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        cancellationTokenSource.Token.Register(s => ((TaskCompletionSource<bool>)s!).SetResult(true), taskCompletionSource);

        await taskCompletionSource.Task;
    }
}
