using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.GameData.Entity;
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

public class BasicDungeonClaimServiceTests
{
    private static BasicDungeonClaimService BuildSut(
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

        return new BasicDungeonClaimService(
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

    public class 경과_시간이_0일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _playerSessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _playerWeaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _playerSkillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 경과_시간이_0일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));
            // LastCalculatedAt = UtcNow → 경과 시간 0
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L, maxStage: 1, lastCalculatedAt: DateTime.UtcNow));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
        }

        [Fact]
        public async Task 보상이_0으로_반환된다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository,
                resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository,
                sessionRepo: _playerSessionRepository,
                weaponRepo: _playerWeaponRepository,
                skillRepo: _playerSkillRepository,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(JobType.Warrior);

            result.EarnedGold.Should().Be(0);
            result.EarnedXp.Should().Be(0);
        }
    }

    public class 오프라인_시간이_있을_때
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

        public 오프라인_시간이_있을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));
            // 1시간 전에 마지막 정산
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L, maxStage: 1, lastCalculatedAt: DateTime.UtcNow.AddHours(-1)));
            _playerSessionRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerSession.Create(1L));
            _playerWeaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _playerSkillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);

            var stageData = StageData.Create(1, monsterHp: 50, monsterAtk: 10, xpPerSecond: 5, goldPerSecond: 10);
            _gameDataCacheService.GetStageDataAsync(1).Returns(stageData);
            _gameDataCacheService.GetJobBaseStatAsync(JobType.Warrior)
                .Returns(JobBaseStat.Create(JobType.Warrior, 1000, 200, 0.1, 1.5, 50, 10));
            _gameDataCacheService.GetSkillDataByJobAsync(Arg.Any<JobType>()).Returns([]);
            _levelUpService.ApplyExpAsync(Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<long>())
                .Returns([]);
            _transactionRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task 경과시간_만큼_보상이_지급된다()
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

            result.EarnedGold.Should().BeGreaterThan(0);
            result.EarnedXp.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task DB와_Redis_캐시가_업데이트된다()
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

            await _transactionRunner.Received(1).ExecuteAsync(Arg.Any<Func<Task>>());
            await _playerRedisRepository.Received(1).DeleteAsync(1L, JobType.Warrior);
        }
    }
}
