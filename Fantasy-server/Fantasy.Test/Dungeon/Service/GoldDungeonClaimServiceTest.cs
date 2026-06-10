using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Entity;
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
        IRandomProvider? randomProvider = null,
        IAppDbTransactionRunner? txRunner = null,
        ICurrentUserProvider? userProvider = null) =>
        new(
            playerRepo ?? Substitute.For<IPlayerRepository>(),
            resourceRepo ?? Substitute.For<IPlayerResourceRepository>(),
            stageRepo ?? Substitute.For<IPlayerStageRepository>(),
            sessionRepo ?? Substitute.For<IPlayerSessionRepository>(),
            weaponRepo ?? Substitute.For<IPlayerWeaponRepository>(),
            skillRepo ?? Substitute.For<IPlayerSkillRepository>(),
            redisRepo ?? Substitute.For<IPlayerRedisRepository>(),
            runRepo ?? Substitute.For<IGoldDungeonRunRepository>(),
            randomProvider ?? Substitute.For<IRandomProvider>(),
            txRunner ?? Substitute.For<IAppDbTransactionRunner>(),
            userProvider ?? Substitute.For<ICurrentUserProvider>());

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

            var sut = BuildSut(runRepo: runRepo, userProvider: userProvider);

            // MaxClicks = 15 * 30 = 450, request 451
            await ((Func<Task>)(() => sut.ExecuteAsync(run.Id, new GoldDungeonClaimRequest(451)))).Should()
                .ThrowAsync<BadRequestException>();
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

        public 정상_클레임_요청시()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _run = GoldDungeonRun.Create(accountId: 1L, durationSeconds: 30, maxClicksPerSecond: 15);
            _runRepository.FindByIdAsync(_run.Id).Returns(_run);
            _randomProvider.Next(0, 100).Returns(50); // >= 2, no mithril
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
                randomProvider: _randomProvider, txRunner: _txRunner, userProvider: _currentUserProvider);

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
                randomProvider: _randomProvider, txRunner: _txRunner, userProvider: _currentUserProvider);

            await sut.ExecuteAsync(_run.Id, new GoldDungeonClaimRequest(100));

            await _redisRepository.Received(1).SetPlayerDataAsync(1L, Arg.Any<PlayerDataResponse>());
        }
    }
}
