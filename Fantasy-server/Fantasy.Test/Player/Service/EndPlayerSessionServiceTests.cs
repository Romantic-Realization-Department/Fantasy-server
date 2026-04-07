using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;
using PlayerResourceEntity = Fantasy.Server.Domain.Player.Entity.PlayerResource;

namespace Fantasy.Test.Player.Service;

public class EndPlayerSessionServiceTests
{
    public class 정상_요청일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly EndPlayerSessionService _sut;
        private readonly EndPlayerSessionRequest _request = new(JobType.Warrior, 1, [1, 2], 5000L, 3000L);

        public 정상_요청일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(1L));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResourceEntity.Create(1L));

            _sut = new EndPlayerSessionService(
                _playerRepository, _playerResourceRepository,
                _playerSessionRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task 세션_데이터가_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerSessionRepository.Received(1).UpdateAsync(Arg.Any<PlayerSession>());
        }

        [Fact]
        public async Task Exp가_플레이어에_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRepository.Received(1).UpdateAsync(Arg.Any<PlayerEntity>());
        }

        [Fact]
        public async Task Gold가_재화에_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerResourceRepository.Received(1).UpdateAsync(Arg.Any<PlayerResourceEntity>());
        }

        [Fact]
        public async Task Redis_캐시가_무효화된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRedisRepository.Received(1).DeleteAsync(1L, JobType.Warrior);
        }
    }

    public class Gold_Exp가_null일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly EndPlayerSessionService _sut;
        private readonly EndPlayerSessionRequest _request = new(JobType.Archer, 1, [], null, null);

        public Gold_Exp가_null일_때()
        {
            _currentUserProvider.GetAccountId().Returns(2L);
            _playerRepository.FindByAccountAndJobAsync(2L, JobType.Archer)
                .Returns(PlayerEntity.Create(2L, JobType.Archer));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(2L));

            _sut = new EndPlayerSessionService(
                _playerRepository, _playerResourceRepository,
                _playerSessionRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task 세션_데이터는_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerSessionRepository.Received(1).UpdateAsync(Arg.Any<PlayerSession>());
        }

        [Fact]
        public async Task 플레이어_Exp가_업데이트되지_않는다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRepository.DidNotReceive().UpdateAsync(Arg.Any<PlayerEntity>());
        }

        [Fact]
        public async Task 재화가_업데이트되지_않는다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerResourceRepository.DidNotReceive().UpdateAsync(Arg.Any<PlayerResourceEntity>());
        }

        [Fact]
        public async Task Redis_캐시가_무효화된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRedisRepository.Received(1).DeleteAsync(2L, JobType.Archer);
        }
    }

    public class 플레이어가_존재하지_않을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly EndPlayerSessionService _sut;

        public 플레이어가_존재하지_않을_때()
        {
            _currentUserProvider.GetAccountId().Returns(99L);
            _playerRepository.FindByAccountAndJobAsync(Arg.Any<long>(), Arg.Any<JobType>())
                .Returns((PlayerEntity?)null);

            _sut = new EndPlayerSessionService(
                _playerRepository, _playerResourceRepository,
                _playerSessionRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var request = new EndPlayerSessionRequest(JobType.Mage, 1, [], null, null);

            var act = async () => await _sut.ExecuteAsync(request);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
