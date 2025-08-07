namespace RedisService.Interfaces;

public interface ICacheService
{
    public Task<string?> GetMessage(string key);
    public Task SetMessage(string key, string message);
}