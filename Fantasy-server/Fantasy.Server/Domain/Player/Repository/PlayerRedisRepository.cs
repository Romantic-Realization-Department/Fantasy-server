using System.Text.Json;
using Fantasy.Server.Domain.Player.Dto.Response;
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
        await _db.KeyDeleteAsync(CacheKey(accountId));
    }
}
