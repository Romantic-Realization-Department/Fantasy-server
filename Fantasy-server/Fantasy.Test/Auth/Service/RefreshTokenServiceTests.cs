using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Enum;
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
    // AccountEntity.Create(...)로 생성된 엔티티의 Id는 DB 할당 전이므로 기본값 0
    private const string ValidRefreshToken = "0:valid-refresh-token";

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
        private readonly RefreshTokenRequest _request = new(ValidRefreshToken);

        public 유효한_리프레시_토큰으로_요청할_때()
        {
            _accountRepository.FindByIdAsync(_account.Id).Returns(_account);
            _refreshTokenRepository
                .RotateAsync(_account.Id, ValidRefreshToken, Arg.Any<string>(), TimeSpan.FromDays(30))
                .Returns(RotateResult.Success);
            _jwtProvider.GenerateAccessToken(_account).Returns("new-access-token");
            _jwtProvider.GenerateRefreshToken(Arg.Any<long>()).Returns("new-refresh-token");
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
                .RotateAsync(_account.Id, ValidRefreshToken, "new-refresh-token", TimeSpan.FromDays(30));
        }
    }

    public class 유효하지_않은_토큰_형식일_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;

        public 유효하지_않은_토큰_형식일_때()
        {
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 콜론이_없는_토큰으로_요청_시_UnauthorizedException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(new RefreshTokenRequest("invalidtoken"));

            await act.Should().ThrowAsync<UnauthorizedException>()
                .WithMessage("유효하지 않은 리프레시 토큰입니다.");
        }

        [Fact]
        public async Task accountId가_숫자가_아닌_토큰으로_요청_시_UnauthorizedException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(new RefreshTokenRequest("abc:sometoken"));

            await act.Should().ThrowAsync<UnauthorizedException>()
                .WithMessage("유효하지 않은 리프레시 토큰입니다.");
        }
    }

    public class 계정이_존재하지_않을_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;

        public 계정이_존재하지_않을_때()
        {
            _accountRepository.FindByIdAsync(Arg.Any<long>()).Returns((AccountEntity?)null);
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_UnauthorizedException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(new RefreshTokenRequest(ValidRefreshToken));

            await act.Should().ThrowAsync<UnauthorizedException>()
                .WithMessage("인증 정보를 찾을 수 없습니다.");
        }
    }

    public class RotateAsync가_NotFound를_반환할_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;
        private readonly AccountEntity _account = CreateAccount();

        public RotateAsync가_NotFound를_반환할_때()
        {
            _accountRepository.FindByIdAsync(_account.Id).Returns(_account);
            _refreshTokenRepository
                .RotateAsync(_account.Id, ValidRefreshToken, Arg.Any<string>(), TimeSpan.FromDays(30))
                .Returns(RotateResult.NotFound);
            _jwtProvider.GenerateAccessToken(_account).Returns("new-access-token");
            _jwtProvider.GenerateRefreshToken(Arg.Any<long>()).Returns("new-refresh-token");
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_UnauthorizedException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(new RefreshTokenRequest(ValidRefreshToken));

            await act.Should().ThrowAsync<UnauthorizedException>()
                .WithMessage("리프레시 토큰을 찾을 수 없습니다.");
        }
    }

    public class RotateAsync가_Reused를_반환할_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly RefreshTokenService _sut;
        private readonly AccountEntity _account = CreateAccount();

        public RotateAsync가_Reused를_반환할_때()
        {
            _accountRepository.FindByIdAsync(_account.Id).Returns(_account);
            _refreshTokenRepository
                .RotateAsync(_account.Id, ValidRefreshToken, Arg.Any<string>(), TimeSpan.FromDays(30))
                .Returns(RotateResult.Reused);
            _jwtProvider.GenerateAccessToken(_account).Returns("new-access-token");
            _jwtProvider.GenerateRefreshToken(Arg.Any<long>()).Returns("new-refresh-token");
            _configuration["Jwt:AccessTokenExpirationMinutes"].Returns("15");
            _sut = new RefreshTokenService(_accountRepository, _refreshTokenRepository, _jwtProvider, _configuration);
        }

        [Fact]
        public async Task 토큰_갱신_요청_시_UnauthorizedException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(new RefreshTokenRequest(ValidRefreshToken));

            await act.Should().ThrowAsync<UnauthorizedException>()
                .WithMessage("토큰 재사용이 감지되었습니다.");
        }
    }
}
