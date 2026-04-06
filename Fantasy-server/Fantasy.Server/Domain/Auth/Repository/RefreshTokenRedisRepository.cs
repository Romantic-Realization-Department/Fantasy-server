using Fantasy.Server.Domain.Auth.Enum;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using StackExchange.Redis;

namespace Fantasy.Server.Domain.Auth.Repository;

public class RefreshTokenRedisRepository : IRefreshTokenRedisRepository
{
    private const string Prefix = "fantasy:";
    private static readonly string ReverseKeyPrefix = $"{Prefix}refresh:token:";

    private static readonly LuaScript SaveScript = LuaScript.Prepare(@"
        local old = redis.call('GET', @forwardKey)
        if old then
            redis.call('DEL', @reverseKeyPrefix .. old)
        end
        redis.call('SET', @forwardKey, @token, 'EX', @ttl)
        redis.call('SET', @reverseKey, @id,    'EX', @ttl)
        return 1
    ");

    private static readonly LuaScript RotateScript = LuaScript.Prepare(@"
        local current = redis.call('GET', @forwardKey)
        if not current then
            return 0
        end
        if current ~= @expectedOldToken then
            redis.call('DEL', @forwardKey)
            redis.call('DEL', @reverseKeyPrefix .. current)
            return -1
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

    private static readonly LuaScript DeleteScript = LuaScript.Prepare(@"
        local token = redis.call('GET', @forwardKey)
        if token then
            redis.call('DEL', @reverseKeyPrefix .. token)
        end
        redis.call('DEL', @forwardKey)
        return 1
    ");

    private readonly IDatabase _db;

    public RefreshTokenRedisRepository(IConnectionMultiplexer multiplexer)
    {
        _db = multiplexer.GetDatabase();
    }

    private static string ForwardKey(long id) => $"{Prefix}refresh:{id}";
    private static string ReverseKey(string token) => $"{ReverseKeyPrefix}{token}";

    public async Task SaveAsync(long id, string token, TimeSpan ttl)
    {
        await _db.ScriptEvaluateAsync(SaveScript, new
        {
            forwardKey       = (RedisKey)ForwardKey(id),
            reverseKey       = (RedisKey)ReverseKey(token),
            reverseKeyPrefix = (RedisValue)ReverseKeyPrefix,
            token            = (RedisValue)token,
            id               = (RedisValue)id.ToString(),
            ttl              = (RedisValue)(long)ttl.TotalSeconds
        });
    }

    public async Task<RotateResult> RotateAsync(long id, string expectedOldToken, string newToken, TimeSpan ttl)
    {
        var result = await _db.ScriptEvaluateAsync(RotateScript, new
        {
            forwardKey       = (RedisKey)ForwardKey(id),
            oldReverseKey    = (RedisKey)ReverseKey(expectedOldToken),
            newReverseKey    = (RedisKey)ReverseKey(newToken),
            reverseKeyPrefix = (RedisValue)ReverseKeyPrefix,
            expectedOldToken = (RedisValue)expectedOldToken,
            newToken         = (RedisValue)newToken,
            id               = (RedisValue)id.ToString(),
            ttl              = (RedisValue)(long)ttl.TotalSeconds
        });

        return (RotateResult)(int)(long)result;
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
        if (!long.TryParse(value.ToString(), out var id))
            throw new InvalidOperationException($"Redis 역방향 키에 저장된 값이 올바른 계정 ID 형식이 아닙니다: token={token}");
        return id;
    }

    public async Task DeleteAsync(long id)
    {
        await _db.ScriptEvaluateAsync(DeleteScript, new
        {
            forwardKey       = (RedisKey)ForwardKey(id),
            reverseKeyPrefix = (RedisValue)ReverseKeyPrefix
        });
    }
}
