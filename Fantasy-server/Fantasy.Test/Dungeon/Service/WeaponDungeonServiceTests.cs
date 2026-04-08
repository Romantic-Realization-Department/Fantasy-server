using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Dungeon.Service;

public class WeaponDungeonServiceTests
{
    private static WeaponDungeonService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IPlayerRedisRepository? redisRepo = null,
        IGameDataCacheService? cache = null,
        ICurrentUserProvider? userProvider = null,
        ICombatStatCalculator? calculator = null)
    {
        playerRepo ??= Substitute.For<IPlayerRepository>();
        resourceRepo ??= Substitute.For<IPlayerResourceRepository>();
        stageRepo ??= Substitute.For<IPlayerStageRepository>();
        sessionRepo ??= Substitute.For<IPlayerSessionRepository>();
        weaponRepo ??= Substitute.For<IPlayerWeaponRepository>();
        skillRepo ??= Substitute.For<IPlayerSkillRepository>();
        redisRepo ??= Substitute.For<IPlayerRedisRepository>();
        cache ??= Substitute.For<IGameDataCacheService>();
        userProvider ??= Substitute.For<ICurrentUserProvider>();
        calculator ??= new CombatStatCalculator();

        return new WeaponDungeonService(
            playerRepo, resourceRepo, stageRepo, sessionRepo,
            weaponRepo, skillRepo, redisRepo, cache,
            userProvider, calculator);
    }

    public class 플레이어가_없을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(Arg.Any<long>(), Arg.Any<JobType>())
                .Returns((PlayerEntity?)null);

            var sut = BuildSut(playerRepo: _playerRepository, userProvider: _currentUserProvider);

            var act = async () => await sut.ExecuteAsync(JobType.Warrior);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    public class 전투력이_부족해서_클리어_실패할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IGameDataCacheService _gameDataCacheService = Substitute.For<IGameDataCacheService>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 전투력이_부족해서_클리어_실패할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior)); // 레벨 1, 장비 없음
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            // 몬스터 HP가 매우 높아 클리어 불가 (DPS * 30 < monsterHp)
            var stageData = StageData.Create(1, monsterHp: 10_000_000, monsterAtk: 999, xpPerSecond: 5, goldPerSecond: 10);
            _gameDataCacheService.GetStageDataAsync(1).Returns(stageData);
            _gameDataCacheService.GetJobBaseStatAsync(JobType.Warrior)
                .Returns(JobBaseStat.Create(JobType.Warrior, 1000, 100, 0, 1.5, 10, 10));
            _gameDataCacheService.GetSkillDataByJobAsync(Arg.Any<JobType>()).Returns([]);
        }

        [Fact]
        public async Task Cleared가_false이다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository,
                resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository,
                sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository,
                skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository,
                cache: _gameDataCacheService,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(JobType.Warrior);

            result.Cleared.Should().BeFalse();
        }

        [Fact]
        public async Task 드랍_보상이_없다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository,
                resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository,
                sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository,
                skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository,
                cache: _gameDataCacheService,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(JobType.Warrior);

            result.DroppedWeapons.Should().BeEmpty();
            result.DroppedScrolls.Should().Be(0);
        }

        [Fact]
        public async Task Redis_캐시가_무효화되지_않는다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository,
                resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository,
                sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository,
                skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository,
                cache: _gameDataCacheService,
                userProvider: _currentUserProvider);

            await sut.ExecuteAsync(JobType.Warrior);

            await _playerRedisRepository.DidNotReceive().DeleteAsync(Arg.Any<long>(), Arg.Any<JobType>());
        }
    }

    public class 전투력이_충분해서_클리어_성공할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IGameDataCacheService _gameDataCacheService = Substitute.For<IGameDataCacheService>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 전투력이_충분해서_클리어_성공할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            // 몬스터 HP = 1 → DPS(100) * 30 = 3000 >> 1 → 클리어 가능
            var stageData = StageData.Create(1, monsterHp: 1, monsterAtk: 1, xpPerSecond: 5, goldPerSecond: 10);
            _gameDataCacheService.GetStageDataAsync(1).Returns(stageData);
            _gameDataCacheService.GetJobBaseStatAsync(JobType.Warrior)
                .Returns(JobBaseStat.Create(JobType.Warrior, 1000, 100, 0, 1.5, 10, 10));
            _gameDataCacheService.GetSkillDataByJobAsync(Arg.Any<JobType>()).Returns([]);
            _gameDataCacheService.GetWeaponDataByGradeAsync(Arg.Any<WeaponGrade>()).Returns([]);
        }

        [Fact]
        public async Task Cleared가_true이다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository,
                resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository,
                sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository,
                skillRepo: _playerSkillRepository,
                redisRepo: _playerRedisRepository,
                cache: _gameDataCacheService,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(JobType.Warrior);

            result.Cleared.Should().BeTrue();
        }
    }
}
