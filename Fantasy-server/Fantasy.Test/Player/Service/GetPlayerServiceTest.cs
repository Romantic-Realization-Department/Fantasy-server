using Fantasy.Server.Domain.Player.Dto.Response;
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

public class GetPlayerServiceTest
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
        private readonly GetPlayerService _sut;
        private readonly PlayerDataResponse _cached = new(
            JobType.Warrior, 5L, 3L, null, [], 1000L, 2000L, 0L, 0L, 0L, [], []);

        public 캐시가_있을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRedisRepository.GetPlayerDataAsync(1L).Returns(_cached);

            _sut = new GetPlayerService(
                _playerRepository,
                _playerResourceRepository,
                _playerStageRepository,
                _playerSessionRepository,
                _playerWeaponRepository,
                _playerSkillRepository,
                _playerRedisRepository,
                _currentUserProvider);
        }

        [Fact]
        public async Task 캐시된_데이터가_반환된다()
        {
            var data = await _sut.ExecuteAsync();

            data.Should().Be(_cached);
        }

        [Fact]
        public async Task DB_조회가_발생하지_않는다()
        {
            await _sut.ExecuteAsync();

            await _playerRepository.DidNotReceive().FindByAccountAsync(Arg.Any<long>());
        }
    }

    public class 플레이어가_있을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GetPlayerService _sut;

        public 플레이어가_있을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRedisRepository.GetPlayerDataAsync(1L).Returns((PlayerDataResponse?)null);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResourceEntity.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            _sut = new GetPlayerService(
                _playerRepository,
                _playerResourceRepository,
                _playerStageRepository,
                _playerSessionRepository,
                _playerWeaponRepository,
                _playerSkillRepository,
                _playerRedisRepository,
                _currentUserProvider);
        }

        [Fact]
        public async Task 기존_데이터가_반환된다()
        {
            var data = await _sut.ExecuteAsync();

            data.JobType.Should().Be(JobType.Warrior);
            data.Level.Should().Be(1L);
        }

        [Fact]
        public async Task Redis에_플레이어_데이터가_캐싱된다()
        {
            await _sut.ExecuteAsync();

            await _playerRedisRepository.Received(1).SetPlayerDataAsync(1L, Arg.Any<PlayerDataResponse>());
        }
    }

    public class 플레이어가_없을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GetPlayerService _sut;

        public 플레이어가_없을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRedisRepository.GetPlayerDataAsync(1L).Returns((PlayerDataResponse?)null);
            _playerRepository.FindByAccountAsync(1L).Returns((PlayerEntity?)null);

            _sut = new GetPlayerService(
                _playerRepository,
                _playerResourceRepository,
                _playerStageRepository,
                _playerSessionRepository,
                _playerWeaponRepository,
                _playerSkillRepository,
                _playerRedisRepository,
                _currentUserProvider);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            Func<Task> act = () => _sut.ExecuteAsync();

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
