using IksOks.Web.Application.Concurrency;

namespace IksOks.Web.Tests.Application.Concurrency;

public sealed class MatchOperationLockTests
{
    [Fact]
    public async Task Same_match_operations_are_serialized()
    {
        var operationLock =
            new MatchOperationLock();

        var matchId =
            Guid.NewGuid();

        var first =
            await operationLock.AcquireAsync(
                matchId,
                CancellationToken.None);

        var secondAcquired =
            new TaskCompletionSource<bool>(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

        var secondTask =
            Task.Run(
                async () =>
                {
                    using var second =
                        await operationLock
                            .AcquireAsync(
                                matchId,
                                CancellationToken.None);

                    secondAcquired.SetResult(
                        true);
                });

        await Task.Delay(100);

        Assert.False(
            secondAcquired.Task.IsCompleted);

        first.Dispose();

        await secondAcquired.Task
            .WaitAsync(
                TimeSpan.FromSeconds(2));

        await secondTask;

        Assert.True(
            secondAcquired.Task.IsCompleted);
    }

    [Fact]
    public async Task Different_matches_can_run_concurrently()
    {
        var operationLock =
            new MatchOperationLock();

        var first =
            await operationLock.AcquireAsync(
                Guid.NewGuid(),
                CancellationToken.None);

        var second =
            await operationLock.AcquireAsync(
                Guid.NewGuid(),
                CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);

        first.Dispose();
        second.Dispose();
    }
}