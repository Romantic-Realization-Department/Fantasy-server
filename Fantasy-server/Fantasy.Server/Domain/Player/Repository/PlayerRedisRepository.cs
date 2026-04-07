using System.Text.Json;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using StackExchange.Redis;

namespace Fantasy.Server.Domain.Player.Repository;

public class PlayerRedisRepository : IPlayerRedisRepository
{
    private const string Prefix = "fantasy:player:";

    private readonly IDatabase _db;

    public PlayerRedisRepository(IConnectionMultiplexer multiplexer)
    {
        _db = multiplexer.GetDatabase();
    }

    private static string CacheKey(long accountId, JobType jobType) =>
        $"{Prefix}{accountId}:{jobType}";

    public async Task SetPlayerDataAsync(long accountId, JobType jobType, PlayerDataResponse data)
    {
        var json = JsonSerializer.Serialize(data);
        await _db.StringSetAsync(CacheKey(accountId, jobType), json, TimeSpan.FromMinutes(30));
    }

    public async Task<PlayerDataResponse?> GetPlayerDataAsync(long accountId, JobType jobType)
    {
        var json = await _db.StringGetAsync(CacheKey(accountId, jobType));
        if (!json.HasValue)
            return null;
        return JsonSerializer.Deserialize<PlayerDataResponse>(json.ToString());
    }

    public async Task DeleteAsync(long accountId, JobType jobType)
    {
        await _db.KeyDeleteAsync(CacheKey(accountId, jobType));
    }
}
