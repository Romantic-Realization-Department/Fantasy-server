using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Fantasy.Server.Domain.Account.Entity;
using Fantasy.Server.Domain.Account.Repository.Interface;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Global.Security.Provider;

public class CurrentUserProvider : ICurrentUserProvider
{
    private readonly IAccountRepository _accountRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserProvider(
        IAccountRepository accountRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _accountRepository = accountRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Account> GetAccountAsync()
    {
        var email = GetEmail();

        return await _accountRepository.FindByEmailAsync(email)
            ?? throw new UnauthorizedException("인증 정보를 찾을 수 없습니다.");
    }

    public string GetEmail()
    {
        return GetUser().FindFirstValue(JwtRegisteredClaimNames.Email)
               ?? throw new UnauthorizedException("이메일 클레임을 찾을 수 없습니다.");
    }

    public long GetAccountId()
    {
        var sub = GetUser().FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? throw new UnauthorizedException("사용자 ID 클레임을 찾을 수 없습니다.");

        if (!long.TryParse(sub, out var accountId))
            throw new UnauthorizedException("사용자 ID 클레임이 유효하지 않습니다.");

        return accountId;
    }

    private ClaimsPrincipal GetUser()
    {
        var context = _httpContextAccessor.HttpContext
                      ?? throw new UnauthorizedException("HTTP 컨텍스트를 찾을 수 없습니다.");

        return context.User;
    }
}
