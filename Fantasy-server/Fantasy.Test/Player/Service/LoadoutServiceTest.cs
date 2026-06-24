using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Player.Service;

public class LoadoutServiceTest
{
    private static LoadoutService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IPlayerRedisRepository? redisRepo = null,
        IIdleRewardSettler? settler = null,
        IGameDataCacheService? cache = null,
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
            cache ?? Substitute.For<IGameDataCacheService>(),
            txRunner ?? Substitute.For<IAppDbTransactionRunner>(),
            userProvider ?? Substitute.For<ICurrentUserProvider>());

    public class 플레이어가_없을_때
    {
        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var playerRepo = Substitute.For<IPlayerRepository>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            playerRepo.FindByAccountAsync(Arg.Any<long>()).Returns((PlayerEntity?)null);

            var sut = BuildSut(playerRepo: playerRepo, userProvider: userProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync(new LoadoutRequest(null, [])))).Should()
                .ThrowAsync<NotFoundException>();
        }
    }

    public class 보유하지_않은_무기를_장착할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IIdleRewardSettler _settler = Substitute.For<IIdleRewardSettler>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 보유하지_않은_무기를_장착할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _settler.SettleAsync(Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<PlayerStage>(),
                    Arg.Any<PlayerSession>(), Arg.Any<List<PlayerWeapon>>(), Arg.Any<List<PlayerSkill>>())
                .Returns(new IdleRewardResult(0, 0, 1, []));
        }

        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                settler: _settler, userProvider: _currentUserProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync(new LoadoutRequest(1, [])))).Should()
                .ThrowAsync<BadRequestException>();
        }
    }

    public class 중복된_스킬_ID를_전달할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IIdleRewardSettler _settler = Substitute.For<IIdleRewardSettler>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 중복된_스킬_ID를_전달할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _settler.SettleAsync(Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<PlayerStage>(),
                    Arg.Any<PlayerSession>(), Arg.Any<List<PlayerWeapon>>(), Arg.Any<List<PlayerSkill>>())
                .Returns(new IdleRewardResult(0, 0, 1, []));
        }

        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                settler: _settler, userProvider: _currentUserProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync(new LoadoutRequest(null, [1, 1])))).Should()
                .ThrowAsync<BadRequestException>();
        }
    }

    public class 해금되지_않은_스킬을_장착할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IIdleRewardSettler _settler = Substitute.For<IIdleRewardSettler>();
        private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 해금되지_않은_스킬을_장착할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _settler.SettleAsync(Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<PlayerStage>(),
                    Arg.Any<PlayerSession>(), Arg.Any<List<PlayerWeapon>>(), Arg.Any<List<PlayerSkill>>())
                .Returns(new IdleRewardResult(0, 0, 1, []));
            _cache.GetSkillDataByJobAsync(Arg.Any<JobType>())
                .Returns([SkillData.Create(1, JobType.Warrior, true, 100, null, SkillEffectType.AtkFlat, 1.0)]);
        }

        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                settler: _settler, cache: _cache, userProvider: _currentUserProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync(new LoadoutRequest(null, [1])))).Should()
                .ThrowAsync<BadRequestException>();
        }
    }

    public class 정상_요청시
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IPlayerRedisRepository _redisRepository = Substitute.For<IPlayerRedisRepository>();
        private readonly IIdleRewardSettler _settler = Substitute.For<IIdleRewardSettler>();
        private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 정상_요청시()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>())
                .Returns([PlayerWeapon.Create(1L, 1, 1, 0, 0)]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>())
                .Returns([PlayerSkill.Create(1L, 1, true)]);
            _settler.SettleAsync(Arg.Any<PlayerEntity>(), Arg.Any<PlayerResource>(), Arg.Any<PlayerStage>(),
                    Arg.Any<PlayerSession>(), Arg.Any<List<PlayerWeapon>>(), Arg.Any<List<PlayerSkill>>())
                .Returns(new IdleRewardResult(100L, 50L, 1L, []));
            _cache.GetSkillDataByJobAsync(Arg.Any<JobType>())
                .Returns([SkillData.Create(1, JobType.Warrior, true, 100, null, SkillEffectType.AtkFlat, 1.0)]);
            _txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task LoadoutResponse가_반환된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                redisRepo: _redisRepository, settler: _settler, cache: _cache,
                txRunner: _txRunner, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(new LoadoutRequest(1, [1]));

            result.Changes.Gold.Should().Be(100L);
            result.Changes.Exp.Should().Be(50L);
        }

        [Fact]
        public async Task Redis에_플레이어_데이터가_캐싱된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                redisRepo: _redisRepository, settler: _settler, cache: _cache,
                txRunner: _txRunner, userProvider: _currentUserProvider);

            await sut.ExecuteAsync(new LoadoutRequest(1, [1]));

            await _redisRepository.Received(1).SetPlayerDataAsync(1L, Arg.Any<PlayerDataResponse>());
        }
    }
}
