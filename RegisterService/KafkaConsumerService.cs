using Confluent.Kafka;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using WorkerService1.Dto;
using WorkerService1.Dto.Utils;

namespace WorkerService1;

public class KafkaConsumerService(
    IConsumer<string, string> consumer,
    IConfiguration configuration,
    ILogger<KafkaConsumerService> logger,
    IDistributedCache  distributedCache,
    IServiceProvider serviceProvider,
    LocalQueueWorker queueWorker)
    : BackgroundService
{
    //private LocalQueueWorker queueWorker => serviceProvider.GetService<LocalQueueWorker>();
    private async Task ProcessKafkaMessageAsync(ConsumeResult<string, string> result)
    {
        string key = result.Key;
        if (key == null)
        {
            logger.LogWarning("Invalid key");
            return;
        }
        var distributed = await distributedCache.GetStringAsync(key);
        if (distributed == null || distributed == "[]")
        {
            SimpleMessage sm = JsonConvert.DeserializeObject<  SimpleMessage>(result.Value);
            StoredCasheMessage<SimpleMessage>[] addedValue = new StoredCasheMessage<SimpleMessage>[]{};
            addedValue.Append(new StoredCasheMessage<SimpleMessage>()
            {
                Message = sm, MessageId = Guid.NewGuid()
            });
            var addedMessageRaw = JsonConvert.SerializeObject(addedValue);
            logger.LogInformation($"Adding message into storage cache {addedMessageRaw}");
            await distributedCache.SetStringAsync(key, addedMessageRaw);
            queueWorker.workerQueue.Enqueue(result);
        }
        else
        {
            await AddMessageToExistingCacheValue(result);
        }
    }
    private async Task AddMessageToExistingCacheValue(ConsumeResult<string,string> message)
    {
        logger.LogInformation("Prepare to add message to existing cache value, key={0}, value={1}", 
            message.Key, message.Value);
        SimpleMessage messageValue = JsonConvert.DeserializeObject<SimpleMessage>(message.Value);
        StoredCasheMessage<SimpleMessage> appendItem = new StoredCasheMessage<SimpleMessage>()
        {
            Message = messageValue,
            MessageId = Guid.NewGuid()
        };
        logger.LogInformation("Adding message to existing cache value, key={0}, value={1}, messageDTO = {2}, cachedItem = {3} ", 
            message.Key, message.Value, messageValue.ToJsonString(), appendItem.ToJsonString());
        
        string currentRawValue = await distributedCache.GetStringAsync(message.Key);
        logger.LogInformation("Current cached message for key = {0}: {1}", message.Key, currentRawValue);
        StoredCasheMessage<SimpleMessage>[] currentValue =
            JsonConvert.DeserializeObject<StoredCasheMessage<SimpleMessage>[]>(currentRawValue);
        
        currentValue.Append(appendItem);
        
        await distributedCache.SetStringAsync(message.Key, JsonConvert.SerializeObject(currentValue));
        logger.LogInformation("Cache value updated for key {0}", message.Key);
    }
    
    private async Task DoConsuming(CancellationToken stoppingToken)
    {
        string topic = configuration.GetSection("Kafka:PersonUpdateConsumer:Topic").Value;
        logger.LogInformation($"Subscribing to topic {topic}");
        consumer.Subscribe(topic);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                logger.LogInformation($"Consumed message: {consumeResult.Message.Value}");
                await ProcessKafkaMessageAsync(consumeResult);
            }
            catch (ConsumeException e)
            {
                logger.LogError($"Error consuming message: {e.Error.Reason}");
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Consumer stopped");
            }
        }
        consumer.Close();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueTask = Task.Run(() => queueWorker.StartAsync(stoppingToken), stoppingToken);
        await Task.Run(() => DoConsuming(stoppingToken), stoppingToken);
        queueTask?.Dispose();
    }

    public override void Dispose()
    {
        consumer?.Dispose();
        base.Dispose();
    }
}