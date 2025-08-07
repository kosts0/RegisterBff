using Microsoft.Extensions.Logging;
using RedisService.Interfaces;
namespace RedisService;

public class RedisService : ICacheService
{
    protected IDatabase cache;
    private ILogger<RedisService> logger { get; set; }

    public RedisService(IDatabase distributedCache, ILogger<RedisService> logger)
    {
        cache = distributedCache;
        
        this.logger = logger;
    }

    public async Task<string?> GetMessage(string oid)
    {
        logger.LogInformation("Getting message with oid: {oid}", oid);
        string? message = await cache.GetStringAsync(oid);
        logger.LogInformation("Return message with oid: {oid}, {message}",oid, message);
        return message;
    }

    public async Task SetMessage(string key, string message)
    {
        logger.LogInformation("Setting message with oid: {oid}, {message}", key, message);
        await cache.SetStringAsync(key, message);
        cache.Get();
    }
}