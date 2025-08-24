namespace RegisterServiceV2.OidConsumer.Options;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; init; } = string.Empty;
    public string GroupId { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public bool EnableAutoCommit { get; init; }
    public string AutoOffsetReset { get; init; } = "Earliest"; // Earliest|Latest|Error
    public int MaxPollIntervalMs { get; init; } = 600000;
    public int SessionTimeoutMs { get; init; } = 45000;
}