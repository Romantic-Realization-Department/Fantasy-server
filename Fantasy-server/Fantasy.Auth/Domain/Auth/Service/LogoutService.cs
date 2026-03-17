using Fantasy.Auth.Domain.Auth.Service.Interface;
using Fantasy.Auth.Global.Security.Provider;
using Fantasy.Common.Domain.Auth.Repository;

namespace Fantasy.Auth.Domain.Auth.Service;

public class LogoutService : ILogoutService
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IRefreshTokenRedisRepository _refreshTokenRepository;

    public LogoutService(
        ICurrentUserProvider currentUserProvider,
        IRefreshTokenRedisRepository refreshTokenRepository)
    {
        _currentUserProvider = currentUserProvider;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task ExecuteAsync()
    {
        var account = await _currentUserProvider.GetAccountAsync();
        await _refreshTokenRepository.DeleteAsync(account.Id);
    }
}