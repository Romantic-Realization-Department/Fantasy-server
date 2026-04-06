using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Dto.Response;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using Fantasy.Server.Domain.Auth.Service.Interface;
using Fantasy.Server.Global.Security.Jwt;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Auth.Service;

public class RefreshTokenService : IRefreshTokenService
{
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);

    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IRefreshTokenRedisRepository _refreshTokenRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly int _accessTokenExpirationMinutes;

    public RefreshTokenService(
        ICurrentUserProvider currentUserProvider,
        IRefreshTokenRedisRepository refreshTokenRepository,
        IJwtProvider jwtProvider,
        IConfiguration configuration)
    {
        _currentUserProvider = currentUserProvider;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtProvider = jwtProvider;
        _accessTokenExpirationMinutes = int.Parse(
            configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
    }

    public async Task<TokenResponse> ExecuteAsync(RefreshTokenRequest request)
    {
        var account = await _currentUserProvider.GetAccountAsync();

        var stored = await _refreshTokenRepository.FindByIdAsync(account.Id)
            ?? throw new UnauthorizedException("리프레시 토큰을 찾을 수 없습니다.");

        if (stored != request.RefreshToken)
            throw new UnauthorizedException("리프레시 토큰이 올바르지 않습니다.");

        var accessToken = _jwtProvider.GenerateAccessToken(account);
        var refreshToken = _jwtProvider.GenerateRefreshToken();

        await _refreshTokenRepository.SaveAsync(account.Id, refreshToken, RefreshTokenTtl);

        var accessTokenExpiresAt = DateTimeOffset.UtcNow
            .AddMinutes(_accessTokenExpirationMinutes)
            .ToUnixTimeSeconds();

        return new TokenResponse(accessToken, refreshToken, accessTokenExpiresAt);
    }
}
