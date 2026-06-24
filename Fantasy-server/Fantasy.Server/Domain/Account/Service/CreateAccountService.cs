using Fantasy.Server.Domain.Account.Dto.Request;
using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Domain.Account.Service.Interface;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using AccountEntity = Fantasy.Server.Domain.Account.Entity.Account;

namespace Fantasy.Server.Domain.Account.Service;

public class CreateAccountService : ICreateAccountService
{
    private readonly IAccountRepository _accountRepository;

    public CreateAccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task ExecuteAsync(CreateAccountRequest request)
    {
        if (await _accountRepository.ExistsByEmailAsync(request.Email))
            throw new ConflictException("이미 사용중인 이메일입니다.");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var account = AccountEntity.Create(request.Email, hashedPassword);
        await _accountRepository.SaveAsync(account);
    }
}
