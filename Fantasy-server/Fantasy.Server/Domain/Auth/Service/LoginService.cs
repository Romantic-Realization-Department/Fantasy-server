using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Dto.Response;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using Fantasy.Server.Domain.Auth.Service.Interface;
using Fantasy.Server.Global.Security.Jwt;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Auth.Service;

public class LoginService : ILoginService
{
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);

    private readonly IAccountRepository _accountRepository;
    private readonly IRefreshTokenRedisRepository _refreshTokenRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly int _accessTokenExpirationMinutes;

    public LoginService(
        IAccountRepository accountRepository,
        IRefreshTokenRedisRepository refreshTokenRepository,
        IJwtProvider jwtProvider,
        IConfiguration configuration)
    {
        _accountRepository = accountRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtProvider = jwtProvider;
        _accessTokenExpirationMinutes = int.Parse(
            configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
    }

    public async Task<TokenResponse> ExecuteAsync(LoginRequest request)
    {
        var account = await _accountRepository.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedException("이메일 또는 비밀번호가 올바르지 않습니다.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.Password))
            throw new UnauthorizedException("이메일 또는 비밀번호가 올바르지 않습니다.");

        var accessToken = _jwtProvider.GenerateAccessToken(account);
        var refreshToken = _jwtProvider.GenerateRefreshToken(account.Id);

        await _refreshTokenRepository.SaveAsync(account.Id, refreshToken, RefreshTokenTtl);

        var accessTokenExpiresAt = DateTimeOffset.UtcNow
            .AddMinutes(_accessTokenExpirationMinutes)
            .ToUnixTimeSeconds();

        return new TokenResponse(accessToken, refreshToken, accessTokenExpiresAt);
    }
}
