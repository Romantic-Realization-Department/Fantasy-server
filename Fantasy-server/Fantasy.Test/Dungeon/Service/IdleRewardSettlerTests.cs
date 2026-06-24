using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.LevelUp.Dto.Response;
using Fantasy.Server.Domain.LevelUp.Service.Interface;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Dungeon.Service;

public class IdleRewardSettlerTests
{
    private static readonly JobBaseStat SampleJobStat =
        JobBaseStat.Create(JobType.Warrior, 1000, 100, 0.05, 1.5, 50.0, 10.0);

    private static readonly StageData SampleStageData =
        StageData.Create(1, 1000, 100, 10, 20);

    private static IdleRewardSettler BuildSut(
        IGameDataCacheService? gameDataCache = null,
        ILevelUpService? levelUpService = null,
        ICombatStatCalculator? calculator = null) =>
        new(
            gameDataCache ?? Substitute.For<IGameDataCacheService>(),
            levelUpService ?? Substitute.For<ILevelUpService>(),
            calculator ?? Substitute.For<ICombatStatCalculator>());

    public class 경과_시간이_0_이하일_때
    {
        [Fact]
        public async Task 빈_결과를_반환한다()
        {
            // Arrange
            var player = PlayerEntity.Create(1L, JobType.Warrior);
            var resource = PlayerResource.Create(1L);
            var stage = PlayerStage.Create(1L, 1, DateTime.UtcNow.AddSeconds(10));
            var session = PlayerSession.Create(1L);

            var sut = BuildSut();

            // Act
            var result = await sut.SettleAsync(player, resource, stage, session, [], []);

            // Assert
            result.EarnedGold.Should().Be(0);
            result.EarnedXp.Should().Be(0);
            result.LevelUps.Should().BeEmpty();
        }
    }

    public class 스테이지_데이터가_없을_때
    {
        private readonly IGameDataCacheService _gameDataCache = Substitute.For<IGameDataCacheService>();

        public 스테이지_데이터가_없을_때()
        {
            _gameDataCache.GetStageDataAsync(Arg.Any<long>()).Returns((StageData?)null);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            // Arrange
            var player = PlayerEntity.Create(1L, JobType.Warrior);
            var resource = PlayerResource.Create(1L);
            var stage = PlayerStage.Create(1L, 1, DateTime.UtcNow.AddHours(-1));
            var session = PlayerSession.Create(1L);

            var sut = BuildSut(gameDataCache: _gameDataCache);

            // Act
            Func<Task> act = () => sut.SettleAsync(player, resource, stage, session, [], []);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    public class 직업_기본_스탯이_없을_때
    {
        private readonly IGameDataCacheService _gameDataCache = Substitute.For<IGameDataCacheService>();

        public 직업_기본_스탯이_없을_때()
        {
            _gameDataCache.GetStageDataAsync(Arg.Any<long>()).Returns(SampleStageData);
            _gameDataCache.GetJobBaseStatAsync(Arg.Any<JobType>()).Returns((JobBaseStat?)null);
            _gameDataCache.GetSkillDataByJobAsync(Arg.Any<JobType>()).Returns([]);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            // Arrange
            var player = PlayerEntity.Create(1L, JobType.Warrior);
            var resource = PlayerResource.Create(1L);
            var stage = PlayerStage.Create(1L, 1, DateTime.UtcNow.AddHours(-1));
            var session = PlayerSession.Create(1L);

            var sut = BuildSut(gameDataCache: _gameDataCache);

            // Act
            Func<Task> act = () => sut.SettleAsync(player, resource, stage, session, [], []);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    public class 정상_요청일_때
    {
        private readonly IGameDataCacheService _gameDataCache = Substitute.For<IGameDataCacheService>();
        private readonly ILevelUpService _levelUpService = Substitute.For<ILevelUpService>();
        private readonly ICombatStatCalculator _calculator = Substitute.For<ICombatStatCalculator>();

        public 정상_요청일_때()
        {
            _gameDataCache.GetStageDataAsync(1L).Returns(SampleStageData);
            _gameDataCache.GetJobBaseStatAsync(JobType.Warrior).Returns(SampleJobStat);
            _gameDataCache.GetSkillDataByJobAsync(JobType.Warrior).Returns([]);
            _calculator.Calculate(Arg.Any<long>(), Arg.Any<JobBaseStat>(), Arg.Any<WeaponData?>(), Arg.Any<long>(), Arg.Any<IEnumerable<(SkillData, bool)>>())
                .Returns(new CombatStat(110, 1000, 0.05, 1.5));
            _calculator.CalculateDps(Arg.Any<CombatStat>()).Returns(115.5);
            _levelUpService.ExecuteAsync(Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<long>())
                .Returns([]);
        }

        [Fact]
        public async Task 경과_시간만큼_골드와_XP가_계산된다()
        {
            // Arrange
            var player = PlayerEntity.Create(1L, JobType.Warrior);
            var resource = PlayerResource.Create(1L);
            var lastCalc = DateTime.UtcNow.AddSeconds(-100);
            var stage = PlayerStage.Create(1L, 1, lastCalc);
            var session = PlayerSession.Create(1L);

            var sut = BuildSut(_gameDataCache, _levelUpService, _calculator);

            // Act
            var result = await sut.SettleAsync(player, resource, stage, session, [], []);

            // Assert
            result.EarnedGold.Should().BeGreaterThan(0);
            result.EarnedXp.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task 최대_오프라인_시간을_초과하면_8시간으로_제한된다()
        {
            // Arrange
            var player = PlayerEntity.Create(1L, JobType.Warrior);
            var resource = PlayerResource.Create(1L);
            var stage = PlayerStage.Create(1L, 1, DateTime.UtcNow.AddHours(-24));
            var session = PlayerSession.Create(1L);

            var sut = BuildSut(_gameDataCache, _levelUpService, _calculator);

            // Act
            var result = await sut.SettleAsync(player, resource, stage, session, [], []);

            // Assert
            var maxSeconds = 8 * 60 * 60L;
            result.EarnedGold.Should().Be(SampleStageData.GoldPerSecond * maxSeconds);
            result.EarnedXp.Should().Be(SampleStageData.XpPerSecond * maxSeconds);
        }

        [Fact]
        public async Task 스테이지가_업데이트된다()
        {
            // Arrange
            var player = PlayerEntity.Create(1L, JobType.Warrior);
            var resource = PlayerResource.Create(1L);
            var stage = PlayerStage.Create(1L, 1, DateTime.UtcNow.AddSeconds(-100));
            var session = PlayerSession.Create(1L);

            var sut = BuildSut(_gameDataCache, _levelUpService, _calculator);

            // Act
            await sut.SettleAsync(player, resource, stage, session, [], []);

            // Assert
            stage.LastCalculatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task 레벨업_결과가_포함된다()
        {
            // Arrange
            _levelUpService.ExecuteAsync(Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<long>())
                .Returns([new LevelUpResult(2, 3)]);

            var player = PlayerEntity.Create(1L, JobType.Warrior);
            var resource = PlayerResource.Create(1L);
            var stage = PlayerStage.Create(1L, 1, DateTime.UtcNow.AddSeconds(-100));
            var session = PlayerSession.Create(1L);

            var sut = BuildSut(_gameDataCache, _levelUpService, _calculator);

            // Act
            var result = await sut.SettleAsync(player, resource, stage, session, [], []);

            // Assert
            result.LevelUps.Should().HaveCount(1);
            result.LevelUps[0].NewLevel.Should().Be(2);
        }
    }
}
