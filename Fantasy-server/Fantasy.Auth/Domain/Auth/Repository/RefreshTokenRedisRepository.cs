using Fantasy.Common.Domain.Auth.Repository;
using Microsoft.Extensions.Caching.Distributed;

namespace Fantasy.Auth.Domain.Auth.Repository;

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
        await _cache.SetStringAsync(id.ToString(), token, options);
    }

    public async Task<string?> FindByIdAsync(long id)
    {
        return await _cache.GetStringAsync(id.ToString());
    }

    public async Task DeleteAsync(long id)
    {
        await _cache.RemoveAsync(id.ToString());
    }
}
