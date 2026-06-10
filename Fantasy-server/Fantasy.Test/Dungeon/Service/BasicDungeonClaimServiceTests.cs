using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
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
        IIdleRewardSettler? settler = null,
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
            settler ?? Substitute.For<IIdleRewardSettler>(),
            txRunner ?? Substitute.For<IAppDbTransactionRunner>(),
            userProvider ?? Substitute.For<ICurrentUserProvider>());

    public class 플레이어가_없을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(Arg.Any<long>()).Returns((PlayerEntity?)null);

            var sut = BuildSut(playerRepo: _playerRepository, userProvider: _currentUserProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync())).Should().ThrowAsync<NotFoundException>();
        }
    }

    public class 방치_보상이_0일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IIdleRewardSettler _settler = Substitute.For<IIdleRewardSettler>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 방치_보상이_0일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _settler.SettleAsync(
                    Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<PlayerStage>(),
                    Arg.Any<PlayerSession>(), Arg.Any<List<PlayerWeapon>>(), Arg.Any<List<PlayerSkill>>())
                .Returns(new IdleRewardResult(0L, 0L, 1L, []));
        }

        [Fact]
        public async Task Changes에_Gold와_Exp가_0으로_반환된다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _resourceRepository, stageRepo: _stageRepository,
                sessionRepo: _sessionRepository, weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                settler: _settler, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.Changes.Gold.Should().Be(0L);
            result.Changes.Exp.Should().Be(0L);
        }
    }

    public class 방치_보상이_있을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _redisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IIdleRewardSettler _settler = Substitute.For<IIdleRewardSettler>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 방치_보상이_있을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _settler.SettleAsync(
                    Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<PlayerStage>(),
                    Arg.Any<PlayerSession>(), Arg.Any<List<PlayerWeapon>>(), Arg.Any<List<PlayerSkill>>())
                .Returns(new IdleRewardResult(3600L, 1800L, 2L, []));
            _txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task Changes에_Gold와_Exp가_반환된다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _resourceRepository, stageRepo: _stageRepository,
                sessionRepo: _sessionRepository, weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                redisRepo: _redisRepository, settler: _settler, txRunner: _txRunner, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync();

            result.Changes.Gold.Should().Be(3600L);
            result.Changes.Exp.Should().Be(1800L);
        }

        [Fact]
        public async Task Redis에_플레이어_데이터가_캐싱된다()
        {
            var sut = BuildSut(
                playerRepo: _playerRepository, resourceRepo: _resourceRepository, stageRepo: _stageRepository,
                sessionRepo: _sessionRepository, weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                redisRepo: _redisRepository, settler: _settler, txRunner: _txRunner, userProvider: _currentUserProvider);

            await sut.ExecuteAsync();

            await _redisRepository.Received(1).SetPlayerDataAsync(1L, Arg.Any<PlayerDataResponse>());
        }
    }

    public class 동시_요청으로_저장_트랜잭션이_충돌할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _redisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IIdleRewardSettler _settler = Substitute.For<IIdleRewardSettler>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 동시_요청으로_저장_트랜잭션이_충돌할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _settler.SettleAsync(
                    Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<PlayerStage>(),
                    Arg.Any<PlayerSession>(), Arg.Any<List<PlayerWeapon>>(), Arg.Any<List<PlayerSkill>>())
                .Returns(new IdleRewardResult(3600L, 1800L, 2L, []));
            // xmin 충돌 → AppDbTransactionRunner가 ConflictException으로 변환한 상황
            _txRunner.When(x => x.ExecuteAsync(Arg.Any<Func<Task>>()))
                .Do(_ => throw new ConflictException("동시 요청으로 인해 충돌이 발생했습니다."));
        }

        private BasicDungeonClaimService BuildConflictSut() => new(
            _playerRepository, _resourceRepository, _stageRepository, _sessionRepository,
            _weaponRepository, _skillRepository, _redisRepository, _settler,
            _txRunner, _currentUserProvider);

        [Fact]
        public async Task ConflictException이_전파된다()
        {
            var sut = BuildConflictSut();

            await ((Func<Task>)(() => sut.ExecuteAsync())).Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task 캐시가_갱신되지_않아_보상이_중복되지_않는다()
        {
            var sut = BuildConflictSut();

            await ((Func<Task>)(() => sut.ExecuteAsync())).Should().ThrowAsync<ConflictException>();

            await _redisRepository.DidNotReceive().SetPlayerDataAsync(Arg.Any<long>(), Arg.Any<PlayerDataResponse>());
        }
    }
}
