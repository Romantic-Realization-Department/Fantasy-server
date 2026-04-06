using Fantasy.Server.Domain.Auth.Repository.Interface;
using Microsoft.Extensions.Caching.Distributed;

namespace Fantasy.Server.Domain.Auth.Repository;

public class RefreshTokenRedisRepository : IRefreshTokenRedisRepository
{
    private readonly IDistributedCache _cache;

    public RefreshTokenRedisRepository(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task SaveAsync(long id, string token, TimeSpan ttl)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };
        await _cache.SetStringAsync($"refresh:{id}", token, options);
    }

    public async Task<string?> FindByIdAsync(long id)
        => await _cache.GetStringAsync($"refresh:{id}");

    public async Task DeleteAsync(long id)
        => await _cache.RemoveAsync($"refresh:{id}");
}
