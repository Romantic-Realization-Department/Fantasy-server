using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Fantasy.Auth.Domain.Account.Service.Interface;
using Fantasy.Auth.Global.Security.Provider;
using Fantasy.Common.Domain.Account.Dto.Request;
using Fantasy.Common.Domain.Account.Repository;
using Fantasy.Common.Domain.Auth.Exception;
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
            ?? throw new InvalidCredentialsException();

        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.Password))
            throw new InvalidCredentialsException();

        await _accountRepository.DeleteAsync(account);
    }
}
