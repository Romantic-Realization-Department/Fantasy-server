using Fantasy.Server.Domain.Account.Dto.Request;
using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Domain.Account.Service.Interface;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Account.Service;

public class DeleteAccountService : IDeleteAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IPlayerRepository _playerRepository;
    private readonly IRefreshTokenRedisRepository _refreshTokenRepository;
    private readonly IAppDbTransactionRunner _txRunner;

    public DeleteAccountService(
        IAccountRepository accountRepository,
        ICurrentUserProvider currentUserProvider,
        IPlayerRepository playerRepository,
        IRefreshTokenRedisRepository refreshTokenRepository,
        IAppDbTransactionRunner txRunner)
    {
        _accountRepository = accountRepository;
        _currentUserProvider = currentUserProvider;
        _playerRepository = playerRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _txRunner = txRunner;
    }

    public async Task ExecuteAsync(DeleteAccountRequest request)
    {
        var email = _currentUserProvider.GetEmail();

        var account = await _accountRepository.FindByEmailAsync(email)
            ?? throw new UnauthorizedException("이메일 또는 비밀번호가 올바르지 않습니다.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.Password))
            throw new UnauthorizedException("이메일 또는 비밀번호가 올바르지 않습니다.");

        await _txRunner.ExecuteAsync(async () =>
        {
            await _playerRepository.DeleteByAccountIdAsync(account.Id);
            await _accountRepository.DeleteAsync(account);
        });

        await _refreshTokenRepository.DeleteAsync(account.Id);
    }
}
