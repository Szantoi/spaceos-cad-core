using System.Threading.Tasks;

namespace CabinetBilder.Core.Infrastructure;

public interface IRedisService
{
    Task<string?> GetStringAsync(string key);
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    
    // Distributed Locking
    Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan expiry);
    Task<bool> ReleaseLockAsync(string lockKey, string lockValue);
}

