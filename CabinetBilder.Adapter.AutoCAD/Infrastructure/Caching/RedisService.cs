using System.Threading.Tasks;
using CabinetBilder.Core.Infrastructure;
using StackExchange.Redis;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Caching;

public class RedisService : IRedisService, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisService(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
    }

    public async Task<string?> GetStringAsync(string key)
    {
        return await _db.StringGetAsync(key);
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        await _db.StringSetAsync(key, value, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan expiry)
    {
        // SET lockKey lockValue NX PX expiry
        // NX: only set if it doesn't exist
        return await _db.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
    }

    public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
    {
        // Lua script to ensure atomicity: only delete if the value matches
        string script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

        var result = await _db.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });
        return (int)result == 1;
    }

    public void Dispose()
    {
        _redis.Dispose();
    }
}

