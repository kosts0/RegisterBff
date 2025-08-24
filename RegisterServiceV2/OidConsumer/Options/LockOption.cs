namespace RegisterServiceV2.OidConsumer.Options;

public sealed class LockOptions
{
    public string KeyPrefix { get; init; } = "lock:oid:";
    public int TtlSeconds { get; init; } = 60;
    public bool AutoRenew { get; init; } = true;
    public int RenewPeriodSeconds { get; init; } = 30;
}