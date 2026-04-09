using Fantasy.Server.Domain.Dungeon.Dto.Request;
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

public class NormalDungeonClearServiceTests
{
    private static NormalDungeonClearService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerRedisRepository? redisRepo = null,
        IGameDataCacheService? cache = null,
        ILevelUpService? levelUpService = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null)
    {
        playerRepo ??= Substitute.For<IPlayerRepository>();
        resourceRepo ??= Substitute.For<IPlayerResourceRepository>();
        stageRepo ??= Substitute.For<IPlayerStageRepository>();
        redisRepo ??= Substitute.For<IPlayerRedisRepository>();
        cache ??= Substitute.For<IGameDataCacheService>();
        levelUpService ??= Substitute.For<ILevelUpService>();
        txRunner ??= Substitute.For<IAppDbTransactionRunner>();
        userProvider ??= Substitute.For<ICurrentUserProvider>();

        return new NormalDungeonClearService(
            playerRepo, resourceRepo, stageRepo, redisRepo,
            cache, levelUpService, txRunner, userProvider);
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

            var act = () => sut.ExecuteAsync(JobType.Warrior, new NormalDungeonClearRequest(Stage: 1));

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    public class 아직_도달하지_않은_스테이지를_요청할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 아직_도달하지_않은_스테이지를_요청할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L, maxStage: 1, lastCalculatedAt: DateTime.UtcNow));
        }

        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository,
                resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository,
                userProvider: _currentUserProvider);

            // MaxStage = 1 이므로 Stage 3은 MaxStage + 1(2)을 초과
            var act = () => sut.ExecuteAsync(JobType.Warrior, new NormalDungeonClearRequest(Stage: 3));

            await act.Should().ThrowAsync<BadRequestException>();
        }
    }

    public class 정상_클리어할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _playerResourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _playerStageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerRedisRepository _playerRedisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IGameDataCacheService _gameDataCacheService = Substitute.For<IGameDataCacheService>();
        private readonly ILevelUpService _levelUpService = Substitute.For<ILevelUpService>();
        private readonly IAppDbTransactionRunner _transactionRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 정상_클리어할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAndJobAsync(1L, JobType.Warrior)
                .Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _playerResourceRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerResource.Create(1L));
            _playerStageRepository.FindByPlayerIdAsync(Arg.Any<long>())
                .Returns(PlayerStage.Create(1L, maxStage: 1, lastCalculatedAt: DateTime.UtcNow));

            var stageData = StageData.Create(1, monsterHp: 100, monsterAtk: 10, xpPerSecond: 5, goldPerSecond: 10);
            _gameDataCacheService.GetStageDataAsync(1).Returns(stageData);
            _levelUpService.ExecuteAsync(Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<long>())
                .Returns([]);
            _transactionRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task 스테이지_데이터_기반_보상이_반환된다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository,
                resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository,
                redisRepo: _playerRedisRepository,
                cache: _gameDataCacheService,
                levelUpService: _levelUpService,
                txRunner: _transactionRunner,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(JobType.Warrior, new NormalDungeonClearRequest(Stage: 1));

            // GoldPerSecond(10) * ClearBonusSeconds(60) = 600
            result.EarnedGold.Should().Be(600);
            // XpPerSecond(5) * ClearBonusSeconds(60) = 300
            result.EarnedXp.Should().Be(300);
        }

        [Fact]
        public async Task DB와_Redis_캐시가_업데이트된다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository,
                resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository,
                redisRepo: _playerRedisRepository,
                cache: _gameDataCacheService,
                levelUpService: _levelUpService,
                txRunner: _transactionRunner,
                userProvider: _currentUserProvider);

            await sut.ExecuteAsync(JobType.Warrior, new NormalDungeonClearRequest(Stage: 1));

            await _transactionRunner.Received(1).ExecuteAsync(Arg.Any<Func<Task>>());
            await _playerRedisRepository.Received(1).DeleteAsync(1L, JobType.Warrior);
        }

        [Fact]
        public async Task 새_스테이지_클리어_시_MaxStage가_갱신된다()
        {
            // Stage 2 = MaxStage + 1 → 새 최대 스테이지
            var stageData2 = StageData.Create(2, monsterHp: 150, monsterAtk: 15, xpPerSecond: 8, goldPerSecond: 15);
            _gameDataCacheService.GetStageDataAsync(2).Returns(stageData2);

            var sut = BuildSut(
                playerRepo: _playerRepository,
                resourceRepo: _playerResourceRepository,
                stageRepo: _playerStageRepository,
                redisRepo: _playerRedisRepository,
                cache: _gameDataCacheService,
                levelUpService: _levelUpService,
                txRunner: _transactionRunner,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(JobType.Warrior, new NormalDungeonClearRequest(Stage: 2));

            result.NewMaxStage.Should().Be(2);
        }
    }
}
