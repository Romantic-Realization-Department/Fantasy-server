using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;
using PlayerResourceEntity = Fantasy.Server.Domain.Player.Entity.PlayerResource;

namespace Fantasy.Test.Player.Service;

public class InitPlayerServiceTests
{
    public class 캐시가_있을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly InitPlayerService _sut;
        private readonly InitPlayerRequest _request = new(JobType.Warrior);
        private readonly PlayerDataResponse _cached = new(
            JobType.Warrior, 5L, 3L, null, [], 1000L, 2000L, 0L, 0L, 0L, [], []);

        public 캐시가_있을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRedisRepository.GetPlayerDataAsync(1L, JobType.Warrior).Returns(_cached);

            _sut = new InitPlayerService(
                _playerRepository, _playerResourceRepository,
                _playerStageRepository, _playerSessionRepository,
                _playerWeaponRepository, _playerSkillRepository,
                _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task 캐시된_데이터가_반환된다()
        {
            var (data, _) = await _sut.ExecuteAsync(_request);

            data.Should().Be(_cached);
        }

        [Fact]
        public async Task DB_조회가_발생하지_않는다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRepository.DidNotReceive().FindByAccountAndJobAsync(Arg.Any<long>(), Arg.Any<JobType>());
        }

        [Fact]
        public async Task isNew가_false로_반환된다()
        {
            var (_, isNew) = await _sut.ExecuteAsync(_request);

            isNew.Should().BeFalse();
        }
    }

    public class 신규_플레이어일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly InitPlayerService _sut;
        private readonly InitPlayerRequest _request = new(JobType.Warrior);

        public 신규_플레이어일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRedisRepository.GetPlayerDataAsync(1L, JobType.Warrior).Returns((PlayerDataResponse?)null);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior).Returns((PlayerEntity?)null);
            _playerRepository.SaveAsync(Arg.Any<PlayerEntity>())
                .Returns(callInfo => callInfo.Arg<PlayerEntity>());
            _playerResourceRepository.SaveAsync(Arg.Any<PlayerResourceEntity>())
                .Returns(callInfo => callInfo.Arg<PlayerResourceEntity>());
            _playerStageRepository.SaveAsync(Arg.Any<PlayerStage>())
                .Returns(callInfo => callInfo.Arg<PlayerStage>());
            _playerSessionRepository.SaveAsync(Arg.Any<PlayerSession>())
                .Returns(callInfo => callInfo.Arg<PlayerSession>());
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            _sut = new InitPlayerService(
                _playerRepository, _playerResourceRepository,
                _playerStageRepository, _playerSessionRepository,
                _playerWeaponRepository, _playerSkillRepository,
                _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task 플레이어_데이터가_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRepository.Received(1).SaveAsync(Arg.Any<PlayerEntity>());
        }

        [Fact]
        public async Task 재화_데이터가_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerResourceRepository.Received(1).SaveAsync(Arg.Any<PlayerResourceEntity>());
        }

        [Fact]
        public async Task 스테이지_데이터가_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerStageRepository.Received(1).SaveAsync(Arg.Any<PlayerStage>());
        }

        [Fact]
        public async Task 세션_데이터가_저장된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerSessionRepository.Received(1).SaveAsync(Arg.Any<PlayerSession>());
        }

        [Fact]
        public async Task isNew가_true로_반환된다()
        {
            var (_, isNew) = await _sut.ExecuteAsync(_request);

            isNew.Should().BeTrue();
        }

        [Fact]
        public async Task Redis에_플레이어_데이터가_캐싱된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRedisRepository.Received(1)
                .SetPlayerDataAsync(1L, JobType.Warrior, Arg.Any<PlayerDataResponse>());
        }
    }

    public class 기존_플레이어일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly InitPlayerService _sut;
        private readonly InitPlayerRequest _request = new(JobType.Warrior);

        public 기존_플레이어일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRedisRepository.GetPlayerDataAsync(1L, JobType.Warrior).Returns((PlayerDataResponse?)null);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResourceEntity.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            _sut = new InitPlayerService(
                _playerRepository, _playerResourceRepository,
                _playerStageRepository, _playerSessionRepository,
                _playerWeaponRepository, _playerSkillRepository,
                _playerRedisRepository, _currentUserProvider);
        }

        [Fact]
        public async Task 플레이어_데이터가_저장되지_않는다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRepository.DidNotReceive().SaveAsync(Arg.Any<PlayerEntity>());
        }

        [Fact]
        public async Task isNew가_false로_반환된다()
        {
            var (_, isNew) = await _sut.ExecuteAsync(_request);

            isNew.Should().BeFalse();
        }

        [Fact]
        public async Task 기존_데이터가_반환된다()
        {
            var (data, _) = await _sut.ExecuteAsync(_request);

            data.JobType.Should().Be(JobType.Warrior);
            data.Level.Should().Be(1L);
        }

        [Fact]
        public async Task Redis에_플레이어_데이터가_캐싱된다()
        {
            await _sut.ExecuteAsync(_request);

            await _playerRedisRepository.Received(1)
                .SetPlayerDataAsync(1L, JobType.Warrior, Arg.Any<PlayerDataResponse>());
        }
    }
}
