namespace RegisterServiceV2.OidConsumer.Services;

public interface IDistributedLock
{
    /// <summary>
    /// Tries to acquire a lock. Returns IDisposable for releasing, or null if not acquired.
    /// Implementations should be safe against releasing someone else's lock.
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl, bool autoRenew, TimeSpan? renewPeriod, CancellationToken ct);
}