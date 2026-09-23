using System.Collections.Concurrent;

namespace IksOks.Web.Application.Concurrency;

public sealed class MatchOperationLock
{
    private readonly ConcurrentDictionary<
        Guid,
        SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AcquireAsync(
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var semaphore =
            _locks.GetOrAdd(
                matchId,
                _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(
            cancellationToken);

        return new ReleaseHandle(
            semaphore);
    }

    private sealed class ReleaseHandle
        : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public ReleaseHandle(
            SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            var semaphore =
                Interlocked.Exchange(
                    ref _semaphore,
                    null);

            semaphore?.Release();
        }
    }
}