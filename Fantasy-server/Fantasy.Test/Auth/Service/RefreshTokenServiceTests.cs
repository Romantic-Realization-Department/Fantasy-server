using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using Fantasy.Server.Domain.Auth.Service;
using Fantasy.Server.Global.Security.Jwt;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
using AccountEntity = Fantasy.Server.Domain.Account.Entity.Account;

namespace Fantasy.Test.Auth.Service;

public class RefreshTokenServiceTests
{
    private const string RefreshToken = "valid-refresh-token";
    private const long AccountId = 1L;

    private static AccountEntity CreateAccount()
        => AccountEntity.Create("user@example.com", "hashed_password");

    public class 유효한_리프레시_토큰으로_요청할_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;
        private readonly AccountEntity _account = CreateAccount();
        private readonly RefreshTokenRequest _request = new(RefreshToken);

        public 유효한_리프레시_토큰으로_요청할_때()
        {
            _refreshTokenRepository.FindIdByTokenAsync(RefreshToken).Returns(AccountId);
            _accountRepository.FindByIdAsync(AccountId).Returns(_account);
            _refreshTokenRepository
                .RotateAsync(_account.Id, RefreshToken, Arg.Any<string>(), TimeSpan.FromDays(30))
                .Returns(true);
            _jwtProvider.GenerateAccessToken(_account).Returns("new-access-token");
            _jwtProvider.GenerateRefreshToken().Returns("new-refresh-token");
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_새_AccessToken과_RefreshToken을_반환한다()
        {
            var result = await _sut.ExecuteAsync(_request);

            result.AccessToken.Should().Be("new-access-token");
            result.RefreshToken.Should().Be("new-refresh-token");
            result.AccessTokenExpiresAt.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_RotateAsync가_호출된다()
        {
            await _sut.ExecuteAsync(_request);

            await _refreshTokenRepository.Received(1)
                .RotateAsync(_account.Id, RefreshToken, "new-refresh-token", TimeSpan.FromDays(30));
        }
    }

    public class Redis에_리프레시_토큰이_없을_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;
        private readonly RefreshTokenRequest _request = new("unknown-token");

        public Redis에_리프레시_토큰이_없을_때()
        {
            _refreshTokenRepository.FindIdByTokenAsync("unknown-token").Returns((long?)null);
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_UnauthorizedException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(_request);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }

    public class 계정이_존재하지_않을_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;
        private readonly RefreshTokenRequest _request = new(RefreshToken);

        public 계정이_존재하지_않을_때()
        {
            _refreshTokenRepository.FindIdByTokenAsync(RefreshToken).Returns(AccountId);
            _accountRepository.FindByIdAsync(AccountId).Returns((AccountEntity?)null);
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_UnauthorizedException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(_request);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }

    public class RotateAsync가_실패할_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;
        private readonly AccountEntity _account = CreateAccount();
        private readonly RefreshTokenRequest _request = new(RefreshToken);

        public RotateAsync가_실패할_때()
        {
            _refreshTokenRepository.FindIdByTokenAsync(RefreshToken).Returns(AccountId);
            _accountRepository.FindByIdAsync(AccountId).Returns(_account);
            _refreshTokenRepository
                .RotateAsync(_account.Id, RefreshToken, Arg.Any<string>(), TimeSpan.FromDays(30))
                .Returns(false);
            _jwtProvider.GenerateAccessToken(_account).Returns("new-access-token");
            _jwtProvider.GenerateRefreshToken().Returns("new-refresh-token");
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_UnauthorizedException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(_request);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }
}
