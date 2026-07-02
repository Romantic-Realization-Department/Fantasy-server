using Fantasy.Server.Domain.Account.Dto.Request;
using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Domain.Account.Service;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using AccountEntity = Fantasy.Server.Domain.Account.Entity.Account;

namespace Fantasy.Test.Account.Service;

public class DeleteAccountServiceTests
{
    private const string Email = "test@example.com";
    private const string PlainPassword = "password123";

    private static AccountEntity CreateAccount()
        => AccountEntity.Create(Email, BCrypt.Net.BCrypt.HashPassword(PlainPassword));

    private static IAppDbTransactionRunner CreateTxRunner()
    {
        var txRunner = Substitute.For<IAppDbTransactionRunner>();
        txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        return txRunner;
    }

    public class 유효한_자격증명으로_탈퇴_요청할_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IAppDbTransactionRunner _txRunner = CreateTxRunner();
        private readonly DeleteAccountService _sut;
        private readonly DeleteAccountRequest _request = new(PlainPassword);
        private readonly AccountEntity _account = CreateAccount();

        public 유효한_자격증명으로_탈퇴_요청할_때()
        {
            _currentUserProvider.GetEmail().Returns(Email);
            _accountRepository.FindByEmailAsync(Email).Returns(_account);
            _sut = new DeleteAccountService(
                _accountRepository, _currentUserProvider,
                _playerRepository, _playerRedisRepository, _refreshTokenRepository, _txRunner);
        }

        [Fact]
        public async Task 회원탈퇴_요청_시_계정이_삭제된다()
        {
            await _sut.ExecuteAsync(_request);

            await _accountRepository.Received(1).DeleteAsync(_account);
        }

        [Fact]
        public async Task 회원탈퇴_요청_시_플레이어가_삭제된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRepository.Received(1).DeleteByAccountIdAsync(_account.Id);
        }

        [Fact]
        public async Task 회원탈퇴_요청_시_RefreshToken이_삭제된다()
        {
            await _sut.ExecuteAsync(_request);

            await _refreshTokenRepository.Received(1).DeleteAsync(_account.Id);
        }

        [Fact]
        public async Task 회원탈퇴_요청_시_Player_캐시가_삭제된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRedisRepository.Received(1).DeleteAsync(_account.Id);
        }
    }

    public class 존재하지_않는_계정으로_탈퇴_요청할_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IAppDbTransactionRunner _txRunner = CreateTxRunner();
        private readonly DeleteAccountService _sut;
        private readonly DeleteAccountRequest _request = new(PlainPassword);

        public 존재하지_않는_계정으로_탈퇴_요청할_때()
        {
            _currentUserProvider.GetEmail().Returns(Email);
            _accountRepository.FindByEmailAsync(Email).Returns((AccountEntity?)null);
            _sut = new DeleteAccountService(
                _accountRepository, _currentUserProvider,
                _playerRepository, _playerRedisRepository, _refreshTokenRepository, _txRunner);
        }

        [Fact]
        public async Task 회원탈퇴_요청_시_UnauthorizedException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(_request);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }

    public class 잘못된_비밀번호로_탈퇴_요청할_때
    {
        private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly IAppDbTransactionRunner _txRunner = CreateTxRunner();
        private readonly DeleteAccountService _sut;
        private readonly DeleteAccountRequest _request = new("wrongpassword");
        private readonly AccountEntity _account = CreateAccount();

        public 잘못된_비밀번호로_탈퇴_요청할_때()
        {
            _currentUserProvider.GetEmail().Returns(Email);
            _accountRepository.FindByEmailAsync(Email).Returns(_account);
            _sut = new DeleteAccountService(
                _accountRepository, _currentUserProvider,
                _playerRepository, _playerRedisRepository, _refreshTokenRepository, _txRunner);
        }

        [Fact]
        public async Task 회원탈퇴_요청_시_UnauthorizedException이_발생한다()
        {
            var act = async () => await _sut.ExecuteAsync(_request);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }
}
