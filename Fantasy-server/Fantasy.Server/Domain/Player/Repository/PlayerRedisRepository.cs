using System.Text.Json;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Fantasy.Server.Domain.Player.Repository;

public class PlayerRedisRepository : IPlayerRedisRepository
{
    private const string Prefix = "fantasy:player:";

    private readonly IDatabase _db;
    private readonly ILogger<PlayerRedisRepository> _logger;

    public PlayerRedisRepository(IConnectionMultiplexer multiplexer, ILogger<PlayerRedisRepository> logger)
    {
        _db = multiplexer.GetDatabase();
        _logger = logger;
    }

    private static string CacheKey(long accountId) => $"{Prefix}{accountId}";

    public async Task SetPlayerDataAsync(long accountId, PlayerDataResponse data)
    {
        var json = JsonSerializer.Serialize(data);
        await _db.StringSetAsync(CacheKey(accountId), json, TimeSpan.FromMinutes(30));
    }

    public async Task<PlayerDataResponse?> GetPlayerDataAsync(long accountId)
    {
        var json = await _db.StringGetAsync(CacheKey(accountId));
        if (!json.HasValue)
            return null;
        return JsonSerializer.Deserialize<PlayerDataResponse>(json.ToString());
    }

    public async Task DeleteAsync(long accountId)
    {
        var key = CacheKey(accountId);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
                return;
            }
            catch (RedisException ex) when (attempt < 3)
            {
                _logger.LogWarning(ex, "Redis 캐시 삭제 실패 (시도 {Attempt}/3, key={Key})", attempt, key);
                await Task.Delay(100 * attempt);
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis 캐시 삭제 최종 실패 (key={Key}). DB가 정상 업데이트됐으므로 계속 진행.", key);
            }
        }
    }
}
