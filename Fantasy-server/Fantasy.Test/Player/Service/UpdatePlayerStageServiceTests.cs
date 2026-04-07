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

namespace Fantasy.Test.Player.Service;

public class UpdatePlayerStageServiceTests
{
    public class 정상_요청일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly UpdatePlayerStageService _sut;
        private readonly UpdatePlayerStageRequest _request = new(JobType.Warrior, 5L);

        public 정상_요청일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L));

            _sut = new UpdatePlayerStageService(
                _playerRepository, _playerStageRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task 스테이지가_업데이트된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerStageRepository.Received(1).UpdateAsync(Arg.Any<PlayerStage>());
        }

        [Fact]
        public async Task Redis_캐시가_무효화된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRedisRepository.Received(1).DeleteAsync(1L, JobType.Warrior);
        }
    }

    public class 플레이어가_존재하지_않을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly UpdatePlayerStageService _sut;

        public 플레이어가_존재하지_않을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(Arg.Any<long>(), Arg.Any<JobType>())
                .Returns((PlayerEntity?)null);

            _sut = new UpdatePlayerStageService(
                _playerRepository, _playerStageRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var request = new UpdatePlayerStageRequest(JobType.Warrior, 3L);

            var act = async () => await _sut.ExecuteAsync(request);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    public class 스테이지_데이터가_존재하지_않을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly UpdatePlayerStageService _sut;

        public 스테이지_데이터가_존재하지_않을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns((PlayerStage?)null);

            _sut = new UpdatePlayerStageService(
                _playerRepository, _playerStageRepository, _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var request = new UpdatePlayerStageRequest(JobType.Warrior, 3L);

            var act = async () => await _sut.ExecuteAsync(request);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
