using Confluent.Kafka;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using WorkerService1.DbEntity;
using WorkerService1.Dto;
using WorkerService1.Dto.Utils;

namespace WorkerService1;

public class LocalQueueWorker(IDistributedCache distributedCache, ILogger<LocalQueueWorker> logger, 
    IServiceScopeFactory _scopeFactory) : BackgroundService
{
    public Queue<ConsumeResult<string, string>> workerQueue = new();
    //PostgresDbContext  dbContext => service.GetService<PostgresDbContext>();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        await Task.Run(() => ProcessQueueMessageAsync(stoppingToken), stoppingToken);
    }
    
    private async Task ProcessQueueMessageAsync(CancellationToken stoppingToken)
    {
        DateTimeOffset lastTimeNotified = DateTimeOffset.Now;
        while (!stoppingToken.IsCancellationRequested)
        {
            if (workerQueue.Any())
            {
                var message =  workerQueue.Dequeue();
                logger.LogInformation("Start operation with message for keycloak auth..., key={0}, value={1}",
                    message.Key, message.Value);
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
                    dbContext.Users.Add(new User()
                    {
                        Oid = long.Parse(message.Key),
                        LastTimeUpdated = DateTime.Now.ToUniversalTime(),
                        LastUpdateAgent = System.Environment.MachineName
                    });
                    await dbContext.SaveChangesAsync();
                }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                logger.LogInformation("End operation with message for keycloak auth..., key={0}, value={1}", 
                    message.Key, message.Value);
                
                await UpdateQueueFromDistributedCache(message);
            }else if (DateTimeOffset.Now - lastTimeNotified > TimeSpan.FromSeconds(5))
            {
                logger.LogInformation("Queue is empty");
                lastTimeNotified = DateTimeOffset.Now;
            }
        }
    }
    private async Task UpdateQueueFromDistributedCache(ConsumeResult<string, string> consumeResult)
    {
        var currentDistributedRaw = await distributedCache.GetStringAsync(consumeResult.Key);
        StoredCasheMessage<SimpleMessage>[] currentDistributedCacheValue = null;
        if (!string.IsNullOrEmpty(currentDistributedRaw))
        {
            currentDistributedCacheValue = JsonConvert.DeserializeObject<StoredCasheMessage<SimpleMessage>[]>(currentDistributedRaw);
        }

        if (currentDistributedCacheValue == null && !workerQueue.Any(m => m.Key == consumeResult.Key))
        {
            await distributedCache.RemoveAsync(consumeResult.Key);
            return;
        }
        else if(currentDistributedCacheValue != null)
        {
            foreach (var item in currentDistributedCacheValue)
            {
                var cr = new ConsumeResult<string, string>()
                {
                    Message = new Message<string, string>()
                    {
                        Key = consumeResult.Key,
                        Value = item.Message.ToJsonString()
                    }
                };
                workerQueue.Enqueue(cr);
                logger.LogInformation("Message for key {0} with id {1} added to worker queue", consumeResult.Key, item.MessageId);
            }
        }
        distributedCache.SetStringAsync(consumeResult.Key, JsonConvert.SerializeObject(Array.Empty<StoredCasheMessage<SimpleMessage>>()));
    }
}