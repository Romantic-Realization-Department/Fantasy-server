using System.Security.Claims;
using Fantasy.Common.Domain.Account.Entity;
using Fantasy.Common.Domain.Account.Repository;

namespace Fantasy.Auth.Global.Security.Provider;

public class CurrentUserProvider : ICurrentUserProvider
{
    private readonly IAccountRepository _accountRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CurrentUserProvider(
        IAccountRepository accountRepository,
        IHttpContextAccessor httpContextAccessor
        )
    {
        _accountRepository = accountRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Account> GetAccountAsync()
    {
        var email = GetEmail();
        
        return await _accountRepository.FindByEmailAsync(email)
            ?? throw new UnauthorizedAccessException();
    }

    public string GetEmail()
    {
        return GetUser().FindFirstValue(ClaimTypes.Email)
               ?? throw new UnauthorizedAccessException();
    }
    
    private ClaimsPrincipal GetUser()
    {
        var context = _httpContextAccessor.HttpContext
                      ?? throw new UnauthorizedAccessException();

        return context.User;
    }
}