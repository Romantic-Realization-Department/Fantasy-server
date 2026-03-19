using Fantasy.Auth.Domain.Auth.Dto.Request;
using Fantasy.Auth.Domain.Auth.Service.Interface;
using Fantasy.Auth.Global.Security.Jwt;
using Fantasy.Common.Domain.Account.Repository;
using Fantasy.Common.Domain.Auth.Dto.Response;
using Fantasy.Common.Domain.Auth.Repository;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Auth.Domain.Auth.Service;

public class LoginService : ILoginService
{
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);

    private readonly IAccountRepository _accountRepository;
    private readonly IRefreshTokenRedisRepository _refreshTokenRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IConfiguration _configuration;

    public LoginService(
        IAccountRepository accountRepository,
        IRefreshTokenRedisRepository refreshTokenRepository,
        IJwtProvider jwtProvider,
        IConfiguration configuration)
    {
        _accountRepository = accountRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtProvider = jwtProvider;
        _configuration = configuration;
    }

    public async Task<TokenResponse> ExecuteAsync(LoginRequest request)
    {
        var account = await _accountRepository.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedException("이메일 또는 비밀번호가 올바르지 않습니다.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.Password))
            throw new UnauthorizedException("이메일 또는 비밀번호가 올바르지 않습니다.");

        var accessToken = _jwtProvider.GenerateAccessToken(account);
        var refreshToken = _jwtProvider.GenerateRefreshToken();

        await _refreshTokenRepository.SaveAsync(account.Id, refreshToken, RefreshTokenTtl);

        var expirationMinutes = int.Parse(
            _configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
        var accessTokenExpiresAt = DateTimeOffset.UtcNow
            .AddMinutes(expirationMinutes)
            .ToUnixTimeSeconds();

        return new TokenResponse(accessToken, refreshToken, accessTokenExpiresAt);
    }
}
