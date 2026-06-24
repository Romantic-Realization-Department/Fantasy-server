using Fantasy.Server.Domain.Auth.Repository.Interface;
using Fantasy.Server.Domain.Auth.Service;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using NSubstitute;
using Xunit;
using AccountEntity = Fantasy.Server.Domain.Account.Entity.Account;

namespace Fantasy.Test.Auth.Service;

public class LogoutServiceTests
{
    public class 인증된_사용자가_로그아웃_할_때
    {
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly IRefreshTokenRedisRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRedisRepository>();
        private readonly LogoutService _sut;
        private readonly AccountEntity _account = AccountEntity.Create("user@example.com", "hashed_password");

        public 인증된_사용자가_로그아웃_할_때()
        {
            _currentUserProvider.GetAccountAsync().Returns(_account);
            _sut = new LogoutService(_currentUserProvider, _refreshTokenRepository);
        }

        [Fact]
        public async Task 로그아웃_요청_시_Redis에서_리프레시_토큰이_삭제된다()
        {
            await _sut.ExecuteAsync();

            await _refreshTokenRepository.Received(1).DeleteAsync(_account.Id);
        }

        [Fact]
        public async Task 로그아웃_요청_시_현재_사용자_계정을_조회한다()
        {
            await _sut.ExecuteAsync();

            await _currentUserProvider.Received(1).GetAccountAsync();
        }
    }
}
