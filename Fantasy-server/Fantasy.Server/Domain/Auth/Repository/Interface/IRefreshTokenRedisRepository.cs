namespace Fantasy.Server.Domain.Auth.Repository.Interface;

public interface IRefreshTokenRedisRepository
{
    Task SaveAsync(long id, string token, TimeSpan ttl);
    Task<bool> RotateAsync(long id, string expectedOldToken, string newToken, TimeSpan ttl);
    Task<string?> FindByIdAsync(long id);
    Task<long?> FindIdByTokenAsync(string token);
    Task DeleteAsync(long id);
}
