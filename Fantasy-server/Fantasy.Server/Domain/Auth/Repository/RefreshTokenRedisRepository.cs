using Fantasy.Server.Domain.Auth.Enum;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<RefreshTokenRedisRepository> _logger;

    public RefreshTokenRedisRepository(IConnectionMultiplexer multiplexer, ILogger<RefreshTokenRedisRepository> logger)
    {
        _db = multiplexer.GetDatabase();
        _logger = logger;
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
    {
        var key = ForwardKey(id);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
                return;
            }
            catch (RedisException ex) when (attempt < 3)
            {
                _logger.LogWarning(ex, "RefreshToken 캐시 삭제 실패 (시도 {Attempt}/3, key={Key})", attempt, key);
                await Task.Delay(100 * attempt);
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "RefreshToken 캐시 삭제 최종 실패 (key={Key}). DB가 정상 업데이트됐으므로 계속 진행.", key);
            }
        }
    }
}
