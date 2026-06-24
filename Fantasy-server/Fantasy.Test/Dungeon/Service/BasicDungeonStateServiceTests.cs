using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Entity;
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

public class BasicDungeonStateServiceTests
{
    private static BasicDungeonStateService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IGameDataCacheService? gameDataCache = null,
        ICombatStatCalculator? calculator = null,
        ICurrentUserProvider? userProvider = null) =>
        new(
            playerRepo ?? Substitute.For<IPlayerRepository>(),
            stageRepo ?? Substitute.For<IPlayerStageRepository>(),
            sessionRepo ?? Substitute.For<IPlayerSessionRepository>(),
            weaponRepo ?? Substitute.For<IPlayerWeaponRepository>(),
            skillRepo ?? Substitute.For<IPlayerSkillRepository>(),
            gameDataCache ?? Substitute.For<IGameDataCacheService>(),
            calculator ?? Substitute.For<ICombatStatCalculator>(),
            userProvider ?? Substitute.For<ICurrentUserProvider>());

    public class 플레이어가_없을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            // Arrange
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(Arg.Any<long>()).Returns((PlayerEntity?)null);

            var sut = BuildSut(playerRepo: _playerRepository, userProvider: _currentUserProvider);

            // Act
            Func<Task> act = () => sut.ExecuteAsync();

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    public class 스테이지_데이터가_없을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 스테이지_데이터가_없을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns((PlayerStage?)null);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            // Arrange
            var sut = BuildSut(playerRepo: _playerRepository, stageRepo: _stageRepository, userProvider: _currentUserProvider);

            // Act
            Func<Task> act = () => sut.ExecuteAsync();

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    public class 세션_데이터가_없을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 세션_데이터가_없을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns((PlayerSession?)null);
        }

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            // Arrange
            var sut = BuildSut(
                playerRepo: _playerRepository,
                stageRepo: _stageRepository,
                sessionRepo: _sessionRepository,
                userProvider: _currentUserProvider);

            // Act
            Func<Task> act = () => sut.ExecuteAsync();

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    public class 정상_요청일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IGameDataCacheService _gameDataCache = Substitute.For<IGameDataCacheService>();
        private readonly ICombatStatCalculator _calculator = Substitute.For<ICombatStatCalculator>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 정상_요청일_때()
        {
            var jobStat = JobBaseStat.Create(JobType.Warrior, 1000, 100, 0.05, 1.5, 50.0, 10.0);
            var stageData = StageData.Create(1, 1000, 100, 10, 20);

            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _gameDataCache.GetStageDataAsync(1L).Returns(stageData);
            _gameDataCache.GetJobBaseStatAsync(JobType.Warrior).Returns(jobStat);
            _gameDataCache.GetSkillDataByJobAsync(JobType.Warrior).Returns([]);
            _calculator.Calculate(Arg.Any<long>(), Arg.Any<JobBaseStat>(), Arg.Any<WeaponData?>(), Arg.Any<long>(), Arg.Any<IEnumerable<(SkillData, bool)>>())
                .Returns(new CombatStat(110, 1000, 0.05, 1.5));
            _calculator.CalculateDps(Arg.Any<CombatStat>()).Returns(115.5);
        }

        [Fact]
        public async Task BasicDungeonStateResponse가_반환된다()
        {
            // Arrange
            var sut = BuildSut(
                playerRepo: _playerRepository,
                stageRepo: _stageRepository,
                sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository,
                skillRepo: _skillRepository,
                gameDataCache: _gameDataCache,
                calculator: _calculator,
                userProvider: _currentUserProvider);

            // Act
            var result = await sut.ExecuteAsync();

            // Assert
            result.Stage.Should().Be(1);
            result.CombatPower.Should().Be(115.5);
            result.GoldPerSecond.Should().Be(20);
            result.XpPerSecond.Should().Be(10);
            result.MaxOfflineSeconds.Should().Be(8 * 60 * 60);
        }
    }
}
