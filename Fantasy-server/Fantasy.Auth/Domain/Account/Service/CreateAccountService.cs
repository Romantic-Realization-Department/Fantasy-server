using Fantasy.Auth.Domain.Account.Service.Interface;
using Fantasy.Common.Domain.Account.Dto.Request;
using Fantasy.Common.Domain.Account.Exception;
using Fantasy.Common.Domain.Account.Repository;
using AccountEntity = Fantasy.Common.Domain.Account.Entity.Account;

namespace Fantasy.Auth.Domain.Account.Service;

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
            throw new DuplicateEmailException(request.Email);
        
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
        
        var account = AccountEntity.Create(request.Email, hashedPassword);
        await _accountRepository.SaveAsync(account);
    }
}