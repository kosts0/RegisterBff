using Microsoft.Extensions.Options;
using RegisterServiceV2.OidConsumer.Options;
using StackExchange.Redis;

namespace RegisterServiceV2.OidConsumer.Services;

public sealed class RedisDistributedLock(IConnectionMultiplexer mux, IOptions<LockOptions> lockOpts) : IDistributedLock
{
    private readonly IDatabase _db = mux.GetDatabase();
    private readonly LockOptions _opt = lockOpts.Value;
    private readonly string _instanceId = Environment.GetEnvironmentVariable("HOSTNAME") ?? Guid.NewGuid().ToString("n");

    private const string ReleaseScript = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end";

    private const string RenewScript = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('PEXPIRE', KEYS[1], ARGV[2])
        else
            return 0
        end";

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl, bool autoRenew, TimeSpan? renewPeriod, CancellationToken ct)
    {
        var value = $"{_instanceId}:{Guid.NewGuid():n}";
        bool acquired = await _db.StringSetAsync(key, value, ttl, when: When.NotExists);
        if (!acquired) return null;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Timer? renewTimer = null;

        if (autoRenew && renewPeriod is { } rp)
        {
            renewTimer = new Timer(async _ =>
            {
                try
                {
                    var res = (long?)await _db.ScriptEvaluateAsync(
                        RenewScript,
                        new RedisKey[] { key },
                        new RedisValue[] { value, (long)ttl.TotalMilliseconds }
                    );
                    if (res == 0) // lock потерян
                        cts.Cancel();
                }
                catch
                {
                    // swallow errors — попробует снова в следующий тик
                }
            }, null, rp, rp);
        }

        return new AsyncReleaser(key, value, _db, renewTimer, cts);
    }

    private sealed class AsyncReleaser(string key, string value, IDatabase db, Timer? timer, CancellationTokenSource linkedCts) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                timer?.Dispose();
                linkedCts.Cancel();

                await db.ScriptEvaluateAsync(
                    ReleaseScript,
                    new RedisKey[] { key },
                    new RedisValue[] { value }
                );
            }
            catch
            {
                // ignore
            }
            finally
            {
                linkedCts.Dispose();
            }
        }
    }
}