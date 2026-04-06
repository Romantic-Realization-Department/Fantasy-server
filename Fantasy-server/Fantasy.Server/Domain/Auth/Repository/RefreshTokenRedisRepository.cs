using Fantasy.Server.Domain.Auth.Enum;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using StackExchange.Redis;

namespace Fantasy.Server.Domain.Auth.Repository;

public class RefreshTokenRedisRepository : IRefreshTokenRedisRepository
{
    private const string Prefix = "fantasy:";

    private static readonly LuaScript SaveScript = LuaScript.Prepare(@"
        redis.call('SET', @forwardKey, @token, 'EX', @ttl)
        return 1
    ");

    private static readonly LuaScript RotateScript = LuaScript.Prepare(@"
        local current = redis.call('GET', @forwardKey)
        if not current then
            return 0
        end
        if current ~= @expectedOldToken then
            redis.call('DEL', @forwardKey)
            return -1
        end
        redis.call('SET', @forwardKey, @newToken, 'EX', @ttl)
        return 1
    ");

    private readonly IDatabase _db;

    public RefreshTokenRedisRepository(IConnectionMultiplexer multiplexer)
    {
        _db = multiplexer.GetDatabase();
    }

    private static string ForwardKey(long id) => $"{Prefix}refresh:{id}";

    public async Task SaveAsync(long id, string token, TimeSpan ttl)
    {
        await _db.ScriptEvaluateAsync(SaveScript, new
        {
            forwardKey = (RedisKey)ForwardKey(id),
            token      = (RedisValue)token,
            ttl        = (RedisValue)(long)ttl.TotalSeconds
        });
    }

    public async Task<RotateResult> RotateAsync(long id, string expectedOldToken, string newToken, TimeSpan ttl)
    {
        var result = await _db.ScriptEvaluateAsync(RotateScript, new
        {
            forwardKey       = (RedisKey)ForwardKey(id),
            expectedOldToken = (RedisValue)expectedOldToken,
            newToken         = (RedisValue)newToken,
            ttl              = (RedisValue)(long)ttl.TotalSeconds
        });

        return (RotateResult)(int)(long)result;
    }

    public async Task DeleteAsync(long id)
        => await _db.KeyDeleteAsync(ForwardKey(id));
}
