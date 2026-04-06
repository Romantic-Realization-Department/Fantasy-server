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

public class LoginServiceTests
{
    public class 유효한_자격증명일_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly LoginService _sut;
        private readonly LoginRequest _request = new("user@example.com", "password123");

        public 유효한_자격증명일_때()
        {
            var account = AccountEntity.Create(_request.Email, BCrypt.Net.BCrypt.HashPassword(_request.Password));
            _accountRepository.FindByEmailAsync(_request.Email).Returns(account);
            _jwtProvider.GenerateAccessToken(account).Returns("access-token");
            _jwtProvider.GenerateRefreshToken().Returns("refresh-token");
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new LoginService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 로그인_요청_시_AccessToken과_RefreshToken을_반환한다()
        {
            var result = await _sut.ExecuteAsync(_request);

            result.AccessToken.Should().Be("access-token");
            result.RefreshToken.Should().Be("refresh-token");
            result.AccessTokenExpiresAt.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task 로그인_요청_시_리프레시_토큰이_Redis에_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _refreshTokenRepository.Received(1)
                .SaveAsync(Arg.Any<long>(), "refresh-token", TimeSpan.FromDays(30));
        }
    }

    public class 존재하지_않는_이메일일_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly LoginService _sut;
        private readonly LoginRequest _request = new("notfound@example.com", "password123");

        public 존재하지_않는_이메일일_때()
        {
            _accountRepository.FindByEmailAsync(_request.Email).Returns((AccountEntity?)null);
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new LoginService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 로그인_요청_시_InvalidCredentialsException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(_request);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }

    public class 잘못된_비밀번호일_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly LoginService _sut;
        private readonly LoginRequest _request = new("user@example.com", "wrong_password");

        public 잘못된_비밀번호일_때()
        {
            var account = AccountEntity.Create(_request.Email, BCrypt.Net.BCrypt.HashPassword("correct_password"));
            _accountRepository.FindByEmailAsync(_request.Email).Returns(account);
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new LoginService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 로그인_요청_시_InvalidCredentialsException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(_request);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }
}
