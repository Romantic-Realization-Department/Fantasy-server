using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using Fantasy.Server.Domain.Auth.Service;
using Fantasy.Server.Global.Security.Jwt;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
using AccountEntity = Fantasy.Server.Domain.Account.Entity.Account;

namespace Fantasy.Test.Auth.Service;

public class RefreshTokenServiceTests
{
    public class 유효한_리프레시_토큰으로_요청할_때
    {
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;
        private readonly AccountEntity _account = AccountEntity.Create("user@example.com", "hashed_password");
        private readonly RefreshTokenRequest _request = new("valid-refresh-token");

        public 유효한_리프레시_토큰으로_요청할_때()
        {
            _currentUserProvider.GetAccountAsync().Returns(_account);
            _refreshTokenRepository.FindByIdAsync(_account.Id).Returns("valid-refresh-token");
            _jwtProvider.GenerateAccessToken(_account).Returns("new-access-token");
            _jwtProvider.GenerateRefreshToken().Returns("new-refresh-token");
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_currentUserProvider, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_새_AccessToken과_RefreshToken을_반환한다()
        {
            // Arrange
            // (생성자에서 설정 완료)

            // Act
            var result = await _sut.ExecuteAsync(_request);

            // Assert
            result.AccessToken.Should().Be("new-access-token");
            result.RefreshToken.Should().Be("new-refresh-token");
            result.AccessTokenExpiresAt.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_새_리프레시_토큰이_Redis에_저장된다()
        {
            // Arrange
            // (생성자에서 설정 완료)

            // Act
            await _sut.ExecuteAsync(_request);

            // Assert
            await _refreshTokenRepository.Received(1)
                .SaveAsync(_account.Id, "new-refresh-token", TimeSpan.FromDays(30));
        }
    }

    public class Redis에_리프레시_토큰이_없을_때
    {
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;
        private readonly AccountEntity _account = AccountEntity.Create("user@example.com", "hashed_password");
        private readonly RefreshTokenRequest _request = new("some-refresh-token");

        public Redis에_리프레시_토큰이_없을_때()
        {
            _currentUserProvider.GetAccountAsync().Returns(_account);
            _refreshTokenRepository.FindByIdAsync(_account.Id).Returns((string?)null);
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_currentUserProvider, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_UnauthorizedException이_발생한다()
        {
            // Arrange
            // (생성자에서 설정 완료)

            // Act
            var act = async () => await _sut.ExecuteAsync(_request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }

    public class 리프레시_토큰이_불일치할_때
    {
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;
        private readonly AccountEntity _account = AccountEntity.Create("user@example.com", "hashed_password");
        private readonly RefreshTokenRequest _request = new("wrong-refresh-token");

        public 리프레시_토큰이_불일치할_때()
        {
            _currentUserProvider.GetAccountAsync().Returns(_account);
            _refreshTokenRepository.FindByIdAsync(_account.Id).Returns("stored-refresh-token");
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_currentUserProvider, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_UnauthorizedException이_발생한다()
        {
            // Arrange
            // (생성자에서 설정 완료)

            // Act
            var act = async () => await _sut.ExecuteAsync(_request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }
}
