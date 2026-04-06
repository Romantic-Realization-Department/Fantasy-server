using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Dto.Response;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using Fantasy.Server.Domain.Auth.Service.Interface;
using Fantasy.Server.Global.Security.Jwt;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Auth.Service;

public class RefreshTokenService : IRefreshTokenService
{
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);

    private readonly IAccountRepository _accountRepository;
    private readonly IRefreshTokenRedisRepository _refreshTokenRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly int _accessTokenExpirationMinutes;

    public RefreshTokenService(
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

    public async Task<TokenResponse> ExecuteAsync(RefreshTokenRequest request)
    {
        var accountId = await _refreshTokenRepository.FindIdByTokenAsync(request.RefreshToken)
            ?? throw new UnauthorizedException("리프레시 토큰을 찾을 수 없습니다.");

        var account = await _accountRepository.FindByIdAsync(accountId)
            ?? throw new UnauthorizedException("인증 정보를 찾을 수 없습니다.");

        var accessToken = _jwtProvider.GenerateAccessToken(account);
        var newRefreshToken = _jwtProvider.GenerateRefreshToken();

        var rotated = await _refreshTokenRepository.RotateAsync(
            account.Id, request.RefreshToken, newRefreshToken, RefreshTokenTtl);

        if (!rotated)
            throw new UnauthorizedException("리프레시 토큰이 이미 사용되었거나 올바르지 않습니다.");

        var accessTokenExpiresAt = DateTimeOffset.UtcNow
            .AddMinutes(_accessTokenExpirationMinutes)
            .ToUnixTimeSeconds();

        return new TokenResponse(accessToken, newRefreshToken, accessTokenExpiresAt);
    }
}
