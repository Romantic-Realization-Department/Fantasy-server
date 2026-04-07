using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.LevelUp.Service.Interface;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Dungeon.Service;

public class BossDungeonServiceTests
{
    private static BossDungeonService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IPlayerRedisRepository? redisRepo = null,
        IGameDataCacheService? cache = null,
        ILevelUpService? levelUpService = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null)
    {
        playerRepo ??= Substitute.For<IPlayerRepository>();
        resourceRepo ??= Substitute.For<IPlayerResourceRepository>();
        stageRepo ??= Substitute.For<IPlayerStageRepository>();
        sessionRepo ??= Substitute.For<IPlayerSessionRepository>();
        weaponRepo ??= Substitute.For<IPlayerWeaponRepository>();
        skillRepo ??= Substitute.For<IPlayerSkillRepository>();
        redisRepo ??= Substitute.For<IPlayerRedisRepository>();
        cache ??= Substitute.For<IGameDataCacheService>();
        levelUpService ??= Substitute.For<ILevelUpService>();
        txRunner ??= Substitute.For<IAppDbTransactionRunner>();
        userProvider ??= Substitute.For<ICurrentUserProvider>();

        return new BossDungeonService(
            playerRepo, resourceRepo, stageRepo, sessionRepo,
            weaponRepo, skillRepo, redisRepo, cache,
            levelUpService, txRunner, userProvider);
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
        private readonly IGameDataCacheService _gameDataCacheService = Substitute.For<IGameDataCacheService>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 전투력이_부족해서_클리어_실패할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior)); // 레벨 1, 아무 장비 없음
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            // 보스 HP: 1_000_000 * 5 → 플레이어 DPS(110)로 클리어 불가
            var stageData = StageData.Create(1, monsterHp: 1_000_000, monsterAtk: 999, xpPerSecond: 5, goldPerSecond: 10);
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
                cache: _gameDataCacheService,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(JobType.Warrior);

            result.Cleared.Should().BeFalse();
        }

        [Fact]
        public async Task 미스릴과_무기_보상이_없다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository,
                resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository,
                sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository,
                skillRepo: _playerSkillRepository,
                cache: _gameDataCacheService,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(JobType.Warrior);

            result.EarnedMithril.Should().Be(0);
            result.DroppedWeapon.Should().BeNull();
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
        private readonly ILevelUpService _levelUpService = Substitute.For<ILevelUpService>();
        private readonly IAppDbTransactionRunner _transactionRunner = Substitute.For<IAppDbTransactionRunner>();
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

            // 보스 HP = 1 * 5 = 5 → DPS(100) > 5 → 클리어 가능
            var stageData = StageData.Create(1, monsterHp: 1, monsterAtk: 1, xpPerSecond: 5, goldPerSecond: 10);
            _gameDataCacheService.GetStageDataAsync(1).Returns(stageData);
            _gameDataCacheService.GetJobBaseStatAsync(JobType.Warrior)
                .Returns(JobBaseStat.Create(JobType.Warrior, 1000, 100, 0, 1.5, 10, 10));
            _gameDataCacheService.GetSkillDataByJobAsync(Arg.Any<JobType>()).Returns([]);
            _gameDataCacheService.GetWeaponDataByGradeAsync(WeaponGrade.A)
                .Returns([WeaponData.Create(10, "A등급검", WeaponGrade.A, JobType.Warrior, 500, 20)]);
            _levelUpService.ApplyExpAsync(Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<long>())
                .Returns([]);
            _transactionRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
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
                levelUpService: _levelUpService,
                txRunner: _transactionRunner,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(JobType.Warrior);

            result.Cleared.Should().BeTrue();
        }

        [Fact]
        public async Task 미스릴이_지급된다()
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
                levelUpService: _levelUpService,
                txRunner: _transactionRunner,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(JobType.Warrior);

            result.EarnedMithril.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Redis_캐시가_무효화된다()
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
                levelUpService: _levelUpService,
                txRunner: _transactionRunner,
                userProvider: _currentUserProvider);

            await sut.ExecuteAsync(JobType.Warrior);

            await _playerRedisRepository.Received(1).DeleteAsync(1L, JobType.Warrior);
        }
    }
}
