using Fantasy.Server.Domain.Auth.Repository.Interface;
using StackExchange.Redis;

namespace Fantasy.Server.Domain.Auth.Repository;

public class RefreshTokenRedisRepository : IRefreshTokenRedisRepository
{
    private const string Prefix = "fantasy:";
    private readonly IDatabase _db;

    public RefreshTokenRedisRepository(IConnectionMultiplexer multiplexer)
    {
        _db = multiplexer.GetDatabase();
    }

    private static string ForwardKey(long id) => $"{Prefix}refresh:{id}";
    private static string ReverseKey(string token) => $"{Prefix}refresh:token:{token}";

    public async Task SaveAsync(long id, string token, TimeSpan ttl)
    {
        var script = LuaScript.Prepare(@"
            local old = redis.call('GET', @forwardKey)
            if old then
                redis.call('DEL', '" + Prefix + @"refresh:token:' .. old)
            end
            redis.call('SET', @forwardKey, @token, 'EX', @ttl)
            redis.call('SET', @reverseKey, @id,    'EX', @ttl)
            return 1
        ");

        await _db.ScriptEvaluateAsync(script, new
        {
            forwardKey = (RedisKey)ForwardKey(id),
            reverseKey = (RedisKey)ReverseKey(token),
            token = (RedisValue)token,
            id    = (RedisValue)id.ToString(),
            ttl   = (RedisValue)(long)ttl.TotalSeconds
        });
    }

    public async Task<bool> RotateAsync(long id, string expectedOldToken, string newToken, TimeSpan ttl)
    {
        var script = LuaScript.Prepare(@"
            local current = redis.call('GET', @forwardKey)
            if not current or current ~= @expectedOldToken then
                return 0
            end
            local reverseId = redis.call('GET', @oldReverseKey)
            if not reverseId or reverseId ~= @id then
                return 0
            end
            redis.call('DEL', @oldReverseKey)
            redis.call('SET', @forwardKey,    @newToken, 'EX', @ttl)
            redis.call('SET', @newReverseKey, @id,       'EX', @ttl)
            return 1
        ");

        var result = await _db.ScriptEvaluateAsync(script, new
        {
            forwardKey    = (RedisKey)ForwardKey(id),
            oldReverseKey = (RedisKey)ReverseKey(expectedOldToken),
            newReverseKey = (RedisKey)ReverseKey(newToken),
            expectedOldToken = (RedisValue)expectedOldToken,
            newToken      = (RedisValue)newToken,
            id            = (RedisValue)id.ToString(),
            ttl           = (RedisValue)(long)ttl.TotalSeconds
        });

        return (long)result == 1;
    }

    public async Task<string?> FindByIdAsync(long id)
    {
        var value = await _db.StringGetAsync(ForwardKey(id));
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<long?> FindIdByTokenAsync(string token)
    {
        var value = await _db.StringGetAsync(ReverseKey(token));
        if (!value.HasValue) return null;
        return long.TryParse(value.ToString(), out var id) ? id : null;
    }

    public async Task DeleteAsync(long id)
    {
        var script = LuaScript.Prepare(@"
            local token = redis.call('GET', @forwardKey)
            if token then
                redis.call('DEL', '" + Prefix + @"refresh:token:' .. token)
            end
            redis.call('DEL', @forwardKey)
            return 1
        ");

        await _db.ScriptEvaluateAsync(script, new
        {
            forwardKey = (RedisKey)ForwardKey(id)
        });
    }
}
