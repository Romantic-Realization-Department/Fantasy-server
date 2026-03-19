using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Fantasy.Auth.Domain.Account.Service.Interface;
using Fantasy.Auth.Global.Security.Provider;
using Fantasy.Common.Domain.Account.Dto.Request;
using Fantasy.Common.Domain.Account.Repository;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Fantasy.Auth.Domain.Account.Service;

public class DeleteAccountService : IDeleteAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public DeleteAccountService(
        IAccountRepository accountRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _accountRepository = accountRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task ExecuteAsync(DeleteAccountRequest request)
    {
        var email = _currentUserProvider.GetEmail();

        var account = await _accountRepository.FindByEmailAsync(email)
            ?? throw new UnauthorizedException("이메일 또는 비밀번호가 올바르지 않습니다.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.Password))
            throw new UnauthorizedException("이메일 또는 비밀번호가 올바르지 않습니다.");

        await _accountRepository.DeleteAsync(account);
    }
}
