namespace Fantasy.Common.Domain.Auth.Repository;

public interface IRefreshTokenRedisRepository
{
    Task SaveAsync(long id, string token, TimeSpan ttl);
    Task<string?> FindByIdAsync(long id);
    Task DeleteAsync(long id);
}