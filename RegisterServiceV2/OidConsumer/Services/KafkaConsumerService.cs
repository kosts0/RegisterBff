using Confluent.Kafka;
using Microsoft.Extensions.Options;
using RegisterServiceV2.OidConsumer.Models;
using RegisterServiceV2.OidConsumer.Options;

namespace RegisterServiceV2.OidConsumer.Services;


public sealed class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _log;
    private readonly IServiceProvider _sp;
    private readonly IDistributedLock _lock;
    private readonly KafkaOptions _kafka;
    private readonly LockOptions _lockOpts;
    public KafkaConsumerService(
        ILogger<KafkaConsumerService> log,
        IServiceProvider sp,
        IDistributedLock distributedLock,
        IOptions<KafkaOptions> kafkaOpt,
        IOptions<LockOptions> lockOpts)
    {
        _log = log;
        _sp = sp;
        _lock = distributedLock;
        _kafka = kafkaOpt.Value;
        _lockOpts = lockOpts.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafka.BootstrapServers,
            GroupId = _kafka.GroupId,
            EnableAutoCommit = _kafka.EnableAutoCommit,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_kafka.AutoOffsetReset, ignoreCase: true),
            MaxPollIntervalMs = _kafka.MaxPollIntervalMs,
            SessionTimeoutMs = _kafka.SessionTimeoutMs,
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
        };
        using var consumer = new ConsumerBuilder<byte[], byte[]>(config)
            .SetErrorHandler((_, e) => _log.LogError("Kafka error: {Err}", e))
            .SetStatisticsHandler((_, json) => _log.LogDebug("Kafka stats: {Stats}", json))
            .Build();
        consumer.Subscribe(_kafka.Topic);
        _log.LogInformation("Subscribed to topic {Topic} in group {Group}", _kafka.Topic, _kafka.GroupId);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<byte[], byte[]> cr;
                try
                {
                    cr = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _log.LogError(ex, "Consume error");
                    continue;
                }


                if (cr?.Message?.Value is null)
                    continue;


                if (!InboundMessage.TryParse(cr.Message.Value, out var msg) || msg is null)
                {
                    _log.LogWarning("Skip malformed message at {TopicPartitionOffset}", cr.TopicPartitionOffset);
// Commit skip only if manual commit is used and you want to move on from bad data
                    if (!_kafka.EnableAutoCommit)
                        consumer.Commit(cr);
                    continue;
                }


                var lockKey = _lockOpts.KeyPrefix + msg.Oid;
                var ttl = TimeSpan.FromSeconds(_lockOpts.TtlSeconds);
                var renew = _lockOpts.AutoRenew;
                var renewPeriod = TimeSpan.FromSeconds(_lockOpts.RenewPeriodSeconds);


// Wait until we can take the lock, but stay responsive to cancellation
                IAsyncDisposable? lease = null;
                while (!stoppingToken.IsCancellationRequested)
                {
                    lease = await _lock.TryAcquireAsync(lockKey, ttl, renew, renewPeriod, stoppingToken);
                    if (lease is not null) break;
                    await Task.Delay(200, stoppingToken); // small backoff; tune as needed
                }


                if (lease is null)
                {
// service stopping
                    break;
                }


                try
                {
                    using var scope = _sp.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IMessageProcessor>();
                    await processor.ProcessAsync(msg, stoppingToken);


                    if (!_kafka.EnableAutoCommit)
                        consumer.Commit(cr); // at-least-once: commit after successful processing
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
// shutting down
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Processing failed for OID {Oid} at {Tpo}", msg.Oid, cr.TopicPartitionOffset);
// Decide: leave uncommitted so it will be retried after rebalance/restart
// Optionally: produce to DLQ here
                }
                finally
                {
                    await lease.DisposeAsync();
                }
            }
        }
        finally
        {
            try
            {
                consumer.Close();
            }
            catch { /* ignore */ }
        }   
    }
}