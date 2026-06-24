using Fantasy.Server.Domain.Auth.Repository.Interface;
using Fantasy.Server.Domain.Auth.Service.Interface;
using Fantasy.Server.Global.Security.Provider;

namespace Fantasy.Server.Domain.Auth.Service;

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
