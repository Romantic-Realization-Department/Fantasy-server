using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.Player.Dto.Response;
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

public class GoldDungeonClaimServiceTest
{
    private static GoldDungeonClaimService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IPlayerRedisRepository? redisRepo = null,
        IGoldDungeonRunRepository? runRepo = null,
        IPlayerDungeonProgressRepository? progressRepo = null,
        IRandomProvider? randomProvider = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null,
        TimeProvider? timeProvider = null) =>
        new(
            playerRepo ?? Substitute.For<IPlayerRepository>(),
            resourceRepo ?? Substitute.For<IPlayerResourceRepository>(),
            stageRepo ?? Substitute.For<IPlayerStageRepository>(),
            sessionRepo ?? Substitute.For<IPlayerSessionRepository>(),
            weaponRepo ?? Substitute.For<IPlayerWeaponRepository>(),
            skillRepo ?? Substitute.For<IPlayerSkillRepository>(),
            redisRepo ?? Substitute.For<IPlayerRedisRepository>(),
            runRepo ?? Substitute.For<IGoldDungeonRunRepository>(),
            progressRepo ?? Substitute.For<IPlayerDungeonProgressRepository>(),
            randomProvider ?? Substitute.For<IRandomProvider>(),
            txRunner ?? Substitute.For<IAppDbTransactionRunner>(),
            userProvider ?? Substitute.For<ICurrentUserProvider>(),
            timeProvider ?? FixedTimeProvider(DateTimeOffset.UtcNow));

    private static TimeProvider FixedTimeProvider(DateTimeOffset now)
    {
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(now);
        return timeProvider;
    }

    private static void SetupPlayerData(
        IPlayerRepository playerRepo,
        IPlayerResourceRepository resourceRepo,
        IPlayerStageRepository stageRepo,
        IPlayerSessionRepository sessionRepo,
        IPlayerWeaponRepository weaponRepo,
        IPlayerSkillRepository skillRepo)
    {
        playerRepo.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
        resourceRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
        stageRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
        sessionRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
        weaponRepo.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
        skillRepo.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
    }

    public class 런이_없을_때
    {
        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var runRepo = Substitute.For<IGoldDungeonRunRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            var runId = Guid.NewGuid();
            userProvider.GetAccountId().Returns(1L);
            runRepo.FindByIdAsync(runId).Returns((GoldDungeonRun?)null);

            var sut = BuildSut(runRepo: runRepo, userProvider: userProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync(runId, new GoldDungeonClaimRequest(10)))).Should()
                .ThrowAsync<NotFoundException>();
        }
    }

    public class 다른_계정의_런일_때
    {
        [Fact]
        public async Task ForbiddenException이_발생한다()
        {
            var runRepo = Substitute.For<IGoldDungeonRunRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            var run = GoldDungeonRun.Create(accountId: 2L, durationSeconds: 30, maxClicksPerSecond: 15);
            runRepo.FindByIdAsync(run.Id).Returns(run);

            var sut = BuildSut(runRepo: runRepo, userProvider: userProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(10)))).Should()
                .ThrowAsync<ForbiddenException>();
        }
    }

    public class 이미_클레임된_런일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IGoldDungeonRunRepository _runRepository = Substitute.For<IGoldDungeonRunRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GoldDungeonRun _claimedRun;

        public 이미_클레임된_런일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _claimedRun = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            _claimedRun.Claim(100, 1000L, 0);
            _runRepository.FindByIdAsync(_claimedRun.Id).Returns(_claimedRun);
            SetupPlayerData(_playerRepository, _resourceRepository, _stageRepository,
                _sessionRepository, _weaponRepository, _skillRepository);
        }

        [Fact]
        public async Task 이전_클레임_결과가_반환된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                runRepo: _runRepository, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(_claimedRun.Id, new GoldDungeonClaimRequest(50));

            result.EarnedGold.Should().Be(1000L);
            result.EarnedMithril.Should().Be(0);
        }
    }

    public class 클릭_횟수_초과시
    {
        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var runRepo = Substitute.For<IGoldDungeonRunRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            var run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            runRepo.FindByIdAsync(run.Id).Returns(run);

            var sut = BuildSut(runRepo: runRepo, userProvider: userProvider,
                timeProvider: FixedTimeProvider(run.StartedAt.AddSeconds(30)));

            // MaxClicks = 15 * 30 = 450, request 451
            await ((Func<Task>)(() => sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(451)))).Should()
                .ThrowAsync<BadRequestException>();
        }
    }

    public class 만료된_런일_때
    {
        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var runRepo = Substitute.For<IGoldDungeonRunRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            var run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            runRepo.FindByIdAsync(run.Id).Returns(run);

            // ExpiresAt = StartedAt + 30 + 30(유예), 그 이후 시점에 claim
            var sut = BuildSut(runRepo: runRepo, userProvider: userProvider,
                timeProvider: FixedTimeProvider(run.ExpiresAt.AddSeconds(1)));

            await ((Func<Task>)(() => sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(10)))).Should()
                .ThrowAsync<BadRequestException>();
        }
    }

    public class 경과_시간_대비_클릭이_과도할_때
    {
        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var runRepo = Substitute.For<IGoldDungeonRunRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            var run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            runRepo.FindByIdAsync(run.Id).Returns(run);

            // 시작 2초 후 허용 상한은 2 * 15 = 30클릭, request 100
            var sut = BuildSut(runRepo: runRepo, userProvider: userProvider,
                timeProvider: FixedTimeProvider(run.StartedAt.AddSeconds(2)));

            await ((Func<Task>)(() => sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(100)))).Should()
                .ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task 허용_상한과_동일한_클릭은_예외가_발생하지_않는다()
        {
            // Arrange
            var runRepo = Substitute.For<IGoldDungeonRunRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            var playerRepo = Substitute.For<IPlayerRepository>();
            var resourceRepo = Substitute.For<IPlayerResourceRepository>();
            var stageRepo = Substitute.For<IPlayerStageRepository>();
            var sessionRepo = Substitute.For<IPlayerSessionRepository>();
            var weaponRepo = Substitute.For<IPlayerWeaponRepository>();
            var skillRepo = Substitute.For<IPlayerSkillRepository>();
            var txRunner = Substitute.For<IAppDbTransactionRunner>();
            userProvider.GetAccountId().Returns(1L);
            var run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            runRepo.FindByIdAsync(run.Id).Returns(run);
            // 2초 경과 → maxAllowedClicks = Ceiling(2 * 450 / 30) = 30, clicks=30 은 허용
            SetupPlayerData(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo);
            txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());

            var sut = BuildSut(playerRepo: playerRepo, resourceRepo: resourceRepo,
                stageRepo: stageRepo, sessionRepo: sessionRepo,
                weaponRepo: weaponRepo, skillRepo: skillRepo,
                runRepo: runRepo, txRunner: txRunner, userProvider: userProvider,
                timeProvider: FixedTimeProvider(run.StartedAt.AddSeconds(2)));

            // Act
            Func<Task> act = () => sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(30));

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task t0에서_클릭0은_예외가_발생하지_않는다()
        {
            // Arrange
            var runRepo = Substitute.For<IGoldDungeonRunRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            var playerRepo = Substitute.For<IPlayerRepository>();
            var resourceRepo = Substitute.For<IPlayerResourceRepository>();
            var stageRepo = Substitute.For<IPlayerStageRepository>();
            var sessionRepo = Substitute.For<IPlayerSessionRepository>();
            var weaponRepo = Substitute.For<IPlayerWeaponRepository>();
            var skillRepo = Substitute.For<IPlayerSkillRepository>();
            var txRunner = Substitute.For<IAppDbTransactionRunner>();
            userProvider.GetAccountId().Returns(1L);
            var run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            runRepo.FindByIdAsync(run.Id).Returns(run);
            // elapsedSeconds=0 → maxAllowedClicks=0, clicks=0 은 허용
            SetupPlayerData(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo);
            txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());

            var sut = BuildSut(playerRepo: playerRepo, resourceRepo: resourceRepo,
                stageRepo: stageRepo, sessionRepo: sessionRepo,
                weaponRepo: weaponRepo, skillRepo: skillRepo,
                runRepo: runRepo, txRunner: txRunner, userProvider: userProvider,
                timeProvider: FixedTimeProvider(run.StartedAt));

            // Act
            Func<Task> act = () => sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(0));

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task t0에서_클릭1은_BadRequestException이_발생한다()
        {
            // Arrange
            var runRepo = Substitute.For<IGoldDungeonRunRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            var run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            runRepo.FindByIdAsync(run.Id).Returns(run);
            // elapsedSeconds=0 → maxAllowedClicks=0, clicks=1 은 거부

            var sut = BuildSut(runRepo: runRepo, userProvider: userProvider,
                timeProvider: FixedTimeProvider(run.StartedAt));

            // Act
            Func<Task> act = () => sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(1));

            // Assert
            await act.Should().ThrowAsync<BadRequestException>();
        }
    }

    public class 만료_경계_케이스
    {
        [Fact]
        public async Task ExpiresAt_정각에는_예외가_발생하지_않는다()
        {
            // Arrange
            var runRepo = Substitute.For<IGoldDungeonRunRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            var playerRepo = Substitute.For<IPlayerRepository>();
            var resourceRepo = Substitute.For<IPlayerResourceRepository>();
            var stageRepo = Substitute.For<IPlayerStageRepository>();
            var sessionRepo = Substitute.For<IPlayerSessionRepository>();
            var weaponRepo = Substitute.For<IPlayerWeaponRepository>();
            var skillRepo = Substitute.For<IPlayerSkillRepository>();
            var txRunner = Substitute.For<IAppDbTransactionRunner>();
            userProvider.GetAccountId().Returns(1L);
            var run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            runRepo.FindByIdAsync(run.Id).Returns(run);
            // now == ExpiresAt → now > ExpiresAt 은 false → 만료 아님
            // elapsedSeconds = Clamp(60, 0, 30) = 30, maxAllowedClicks = 450, clicks=100 허용
            SetupPlayerData(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo);
            txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());

            var sut = BuildSut(playerRepo: playerRepo, resourceRepo: resourceRepo,
                stageRepo: stageRepo, sessionRepo: sessionRepo,
                weaponRepo: weaponRepo, skillRepo: skillRepo,
                runRepo: runRepo, txRunner: txRunner, userProvider: userProvider,
                timeProvider: FixedTimeProvider(run.ExpiresAt));

            // Act
            Func<Task> act = () => sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(100));

            // Assert
            await act.Should().NotThrowAsync();
        }
    }

    public class 정상_클레임_요청시
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _redisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IGoldDungeonRunRepository _runRepository = Substitute.For<IGoldDungeonRunRepository>();
        private readonly IRandomProvider _randomProvider = Substitute.For<IRandomProvider>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GoldDungeonRun _run;
        private readonly TimeProvider _timeProvider;

        public 정상_클레임_요청시()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            _runRepository.FindByIdAsync(_run.Id).Returns(_run);
            _randomProvider.Next(0, 100).Returns(50); // >= 2, no mithril
            _timeProvider = FixedTimeProvider(_run.StartedAt.AddSeconds(30));
            SetupPlayerData(_playerRepository, _resourceRepository, _stageRepository,
                _sessionRepository, _weaponRepository, _skillRepository);
            _txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task 클릭당_10골드가_지급된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                redisRepo: _redisRepository, runRepo: _runRepository,
                randomProvider: _randomProvider, txRunner: _txRunner, userProvider: _currentUserProvider,
                timeProvider: _timeProvider);

            var result = await sut.ExecuteAsync(_run.Id, new GoldDungeonClaimRequest(100));

            result.EarnedGold.Should().Be(1000L);
        }

        [Fact]
        public async Task Redis에_플레이어_데이터가_캐싱된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                redisRepo: _redisRepository, runRepo: _runRepository,
                randomProvider: _randomProvider, txRunner: _txRunner, userProvider: _currentUserProvider,
                timeProvider: _timeProvider);

            await sut.ExecuteAsync(_run.Id, new GoldDungeonClaimRequest(100));

            await _redisRepository.Received(1).SetPlayerDataAsync(1L, Arg.Any<PlayerDataResponse>());
        }
    }

    public class 동시_claim으로_저장_트랜잭션이_충돌할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _redisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IGoldDungeonRunRepository _runRepository = Substitute.For<IGoldDungeonRunRepository>();
        private readonly IRandomProvider _randomProvider = Substitute.For<IRandomProvider>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GoldDungeonRun _run;
        private readonly TimeProvider _timeProvider;

        public 동시_claim으로_저장_트랜잭션이_충돌할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            _runRepository.FindByIdAsync(_run.Id).Returns(_run);
            _randomProvider.Next(0, 100).Returns(50);
            _timeProvider = FixedTimeProvider(_run.StartedAt.AddSeconds(30));
            SetupPlayerData(_playerRepository, _resourceRepository, _stageRepository,
                _sessionRepository, _weaponRepository, _skillRepository);
            // xmin 충돌 → AppDbTransactionRunner가 ConflictException으로 변환한 상황
            _txRunner.When(x => x.ExecuteAsync(Arg.Any<Func<Task>>()))
                .Do(_ => throw new ConflictException("동시 요청으로 인해 충돌이 발생했습니다."));
        }

        private GoldDungeonClaimService BuildConflictSut() => BuildSut(
            playerRepo: _playerRepository, resourceRepo: _resourceRepository,
            stageRepo: _stageRepository, sessionRepo: _sessionRepository,
            weaponRepo: _weaponRepository, skillRepo: _skillRepository,
            redisRepo: _redisRepository, runRepo: _runRepository,
            randomProvider: _randomProvider, txRunner: _txRunner, userProvider: _currentUserProvider,
            timeProvider: _timeProvider);

        [Fact]
        public async Task ConflictException이_전파된다()
        {
            var sut = BuildConflictSut();

            await ((Func<Task>)(() => sut.ExecuteAsync(_run.Id, new GoldDungeonClaimRequest(100)))).Should()
                .ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task 캐시가_갱신되지_않아_보상이_중복되지_않는다()
        {
            var sut = BuildConflictSut();

            await ((Func<Task>)(() => sut.ExecuteAsync(_run.Id, new GoldDungeonClaimRequest(100)))).Should()
                .ThrowAsync<ConflictException>();

            await _redisRepository.DidNotReceive().SetPlayerDataAsync(Arg.Any<long>(), Arg.Any<PlayerDataResponse>());
        }
    }

    public class 정상_클레임시_HighScore가_갱신될_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IGoldDungeonRunRepository _runRepository = Substitute.For<IGoldDungeonRunRepository>();
        private readonly IPlayerDungeonProgressRepository _progressRepository = Substitute.For<IPlayerDungeonProgressRepository>();
        private readonly IRandomProvider _randomProvider = Substitute.For<IRandomProvider>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
        private readonly GoldDungeonRun _run;
        private readonly TimeProvider _timeProvider;

        public 정상_클레임시_HighScore가_갱신될_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            _runRepository.FindByIdAsync(_run.Id).Returns(_run);
            _randomProvider.Next(0, 100).Returns(50);
            _timeProvider = FixedTimeProvider(_run.StartedAt.AddSeconds(30));
            SetupPlayerData(_playerRepository, _resourceRepository, _stageRepository,
                _sessionRepository, _weaponRepository, _skillRepository);

            var existingProgress = PlayerDungeonProgress.Create(1L, DungeonType.Gold);
            existingProgress.UpdateHighScore(500L);
            _progressRepository.FindByPlayerIdAndDungeonTypeAsync(Arg.Any<long>(), DungeonType.Gold)
                .Returns(existingProgress);

            _txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task 기존_최고점수보다_높으면_HighScore가_갱신된다()
        {
            // 클릭 100 * 10골드 = 1000 > 기존 500
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                runRepo: _runRepository, progressRepo: _progressRepository,
                randomProvider: _randomProvider, txRunner: _txRunner, userProvider: _currentUserProvider,
                timeProvider: _timeProvider);

            var result = await sut.ExecuteAsync(_run.Id, new GoldDungeonClaimRequest(100));

            result.HighScore.Should().Be(1000L);
        }

        [Fact]
        public async Task 진행도_UpdateAsync가_호출된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                runRepo: _runRepository, progressRepo: _progressRepository,
                randomProvider: _randomProvider, txRunner: _txRunner, userProvider: _currentUserProvider,
                timeProvider: _timeProvider);

            await sut.ExecuteAsync(_run.Id, new GoldDungeonClaimRequest(100));

            await _progressRepository.Received(1).UpdateAsync(Arg.Any<PlayerDungeonProgress>());
        }
    }

    public class 정상_클레임시_기존_최고점수보다_낮을_때
    {
        [Fact]
        public async Task HighScore가_기존값으로_유지된다()
        {
            var playerRepository = Substitute.For<IPlayerRepository>();
            var resourceRepository = Substitute.For<IPlayerResourceRepository>();
            var stageRepository = Substitute.For<IPlayerStageRepository>();
            var sessionRepository = Substitute.For<IPlayerSessionRepository>();
            var weaponRepository = Substitute.For<IPlayerWeaponRepository>();
            var skillRepository = Substitute.For<IPlayerSkillRepository>();
            var runRepository = Substitute.For<IGoldDungeonRunRepository>();
            var progressRepository = Substitute.For<IPlayerDungeonProgressRepository>();
            var randomProvider = Substitute.For<IRandomProvider>();
            var txRunner = Substitute.For<IAppDbTransactionRunner>();
            var currentUserProvider = Substitute.For<ICurrentUserProvider>();

            currentUserProvider.GetAccountId().Returns(1L);
            var run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            runRepository.FindByIdAsync(run.Id).Returns(run);
            randomProvider.Next(0, 100).Returns(50);
            SetupPlayerData(playerRepository, resourceRepository, stageRepository,
                sessionRepository, weaponRepository, skillRepository);

            // 기존 최고점수 5000 > 이번 클릭 10 * 10골드 = 100
            var existingProgress = PlayerDungeonProgress.Create(1L, DungeonType.Gold);
            existingProgress.UpdateHighScore(5000L);
            progressRepository.FindByPlayerIdAndDungeonTypeAsync(Arg.Any<long>(), DungeonType.Gold)
                .Returns(existingProgress);

            txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());

            var sut = BuildSut(playerRepo: playerRepository, resourceRepo: resourceRepository,
                stageRepo: stageRepository, sessionRepo: sessionRepository,
                weaponRepo: weaponRepository, skillRepo: skillRepository,
                runRepo: runRepository, progressRepo: progressRepository,
                randomProvider: randomProvider, txRunner: txRunner, userProvider: currentUserProvider,
                timeProvider: FixedTimeProvider(run.StartedAt.AddSeconds(30)));

            var result = await sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(10));

            result.HighScore.Should().Be(5000L);
        }
    }

    public class 이미_클레임된_런_재조회시_HighScore가_포함될_때
    {
        [Fact]
        public async Task 저장된_HighScore가_반환된다()
        {
            var playerRepository = Substitute.For<IPlayerRepository>();
            var resourceRepository = Substitute.For<IPlayerResourceRepository>();
            var stageRepository = Substitute.For<IPlayerStageRepository>();
            var sessionRepository = Substitute.For<IPlayerSessionRepository>();
            var weaponRepository = Substitute.For<IPlayerWeaponRepository>();
            var skillRepository = Substitute.For<IPlayerSkillRepository>();
            var runRepository = Substitute.For<IGoldDungeonRunRepository>();
            var progressRepository = Substitute.For<IPlayerDungeonProgressRepository>();
            var currentUserProvider = Substitute.For<ICurrentUserProvider>();

            currentUserProvider.GetAccountId().Returns(1L);
            var claimedRun = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            claimedRun.Claim(100, 1000L, 0);
            runRepository.FindByIdAsync(claimedRun.Id).Returns(claimedRun);
            SetupPlayerData(playerRepository, resourceRepository, stageRepository,
                sessionRepository, weaponRepository, skillRepository);

            var existingProgress = PlayerDungeonProgress.Create(1L, DungeonType.Gold);
            existingProgress.UpdateHighScore(2000L);
            progressRepository.FindByPlayerIdAndDungeonTypeAsync(Arg.Any<long>(), DungeonType.Gold)
                .Returns(existingProgress);

            var sut = BuildSut(playerRepo: playerRepository, resourceRepo: resourceRepository,
                stageRepo: stageRepository, sessionRepo: sessionRepository,
                weaponRepo: weaponRepository, skillRepo: skillRepository,
                runRepo: runRepository, progressRepo: progressRepository,
                userProvider: currentUserProvider);

            var result = await sut.ExecuteAsync(claimedRun.Id, new GoldDungeonClaimRequest(50));

            result.HighScore.Should().Be(2000L);
        }
    }
}
