using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Dto.Response;
using Fantasy.Server.Domain.Auth.Enum;
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
        var accountId = ParseAccountId(request.RefreshToken);
        var newRefreshToken = _jwtProvider.GenerateRefreshToken(accountId);

        var rotateResult = await _refreshTokenRepository.RotateAsync(
            accountId, request.RefreshToken, newRefreshToken, RefreshTokenTtl);

        switch (rotateResult)
        {
            case RotateResult.Reused:
                throw new UnauthorizedException("토큰 재사용이 감지되었습니다.");
            case RotateResult.NotFound:
                throw new UnauthorizedException("리프레시 토큰을 찾을 수 없습니다.");
        }

        var account = await _accountRepository.FindByIdAsync(accountId)
            ?? throw new UnauthorizedException("인증 정보를 찾을 수 없습니다.");

        var accessToken = _jwtProvider.GenerateAccessToken(account);

        var accessTokenExpiresAt = DateTimeOffset.UtcNow
            .AddMinutes(_accessTokenExpirationMinutes)
            .ToUnixTimeSeconds();

        return new TokenResponse(accessToken, newRefreshToken, accessTokenExpiresAt);
    }

    private static long ParseAccountId(string token)
    {
        var separatorIndex = token.IndexOf(':');
        if (separatorIndex <= 0)
            throw new UnauthorizedException("유효하지 않은 리프레시 토큰입니다.");

        if (!long.TryParse(token[..separatorIndex], out var accountId))
            throw new UnauthorizedException("유효하지 않은 리프레시 토큰입니다.");

        return accountId;
    }
}
