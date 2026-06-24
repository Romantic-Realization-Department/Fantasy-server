using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Request;
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

public class SkillUnlockServiceTest
{
    private static SkillUnlockService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IPlayerRedisRepository? redisRepo = null,
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

            await ((Func<Task>)(() => sut.ExecuteAsync(new SkillUnlockRequest(1)))).Should()
                .ThrowAsync<NotFoundException>();
        }
    }

    public class 스킬_데이터가_없을_때
    {
        [Fact]
        public async Task NotFoundException이_발생한다()
        {
            var playerRepo = Substitute.For<IPlayerRepository>();
            var cache = Substitute.For<IGameDataCacheService>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            playerRepo.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            cache.GetSkillDataAsync(99).Returns((SkillData?)null);

            var sut = BuildSut(playerRepo: playerRepo, cache: cache, userProvider: userProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync(new SkillUnlockRequest(99)))).Should()
                .ThrowAsync<NotFoundException>();
        }
    }

    public class 다른_직업의_스킬일_때
    {
        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var playerRepo = Substitute.For<IPlayerRepository>();
            var cache = Substitute.For<IGameDataCacheService>();
            var userProvider = Substitute.For<ICurrentUserProvider>();
            userProvider.GetAccountId().Returns(1L);
            playerRepo.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            cache.GetSkillDataAsync(1).Returns(
                SkillData.Create(1, JobType.Mage, true, 100, null, SkillEffectType.AtkFlat, 1.0));

            var sut = BuildSut(playerRepo: playerRepo, cache: cache, userProvider: userProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync(new SkillUnlockRequest(1)))).Should()
                .ThrowAsync<BadRequestException>();
        }
    }

    public class 이미_해금된_스킬일_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerStageRepository _stageRepository = Substitute.For<IPlayerStageRepository>();
        private readonly IPlayerSessionRepository _sessionRepository = Substitute.For<IPlayerSessionRepository>();
        private readonly IPlayerWeaponRepository _weaponRepository = Substitute.For<IPlayerWeaponRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 이미_해금된_스킬일_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _cache.GetSkillDataAsync(1).Returns(
                SkillData.Create(1, JobType.Warrior, true, 100, null, SkillEffectType.AtkFlat, 1.0));
            _skillRepository.FindByPlayerIdAndSkillIdAsync(Arg.Any<long>(), 1)
                .Returns(PlayerSkill.Create(1L, 1, true));
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([PlayerSkill.Create(1L, 1, true)]);
        }

        [Fact]
        public async Task WasAlreadyUnlocked가_true로_반환된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                cache: _cache, userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(new SkillUnlockRequest(1));

            result.WasAlreadyUnlocked.Should().BeTrue();
            result.Changes.Gold.Should().Be(0);
            result.Changes.UnlockedSkillIds.Should().BeEmpty();
        }
    }

    public class 선행_스킬이_해금되지_않았을_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 선행_스킬이_해금되지_않았을_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _cache.GetSkillDataAsync(2).Returns(
                SkillData.Create(2, JobType.Warrior, true, 100, 1, SkillEffectType.AtkFlat, 1.0));
            _skillRepository.FindByPlayerIdAndSkillIdAsync(Arg.Any<long>(), 2)
                .Returns((PlayerSkill?)null);
            _skillRepository.FindByPlayerIdAndSkillIdAsync(Arg.Any<long>(), 1)
                .Returns((PlayerSkill?)null);
        }

        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, skillRepo: _skillRepository,
                cache: _cache, userProvider: _currentUserProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync(new SkillUnlockRequest(2)))).Should()
                .ThrowAsync<BadRequestException>();
        }
    }

    public class SP가_부족할_때
    {
        private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
        private readonly IPlayerResourceRepository _resourceRepository = Substitute.For<IPlayerResourceRepository>();
        private readonly IPlayerSkillRepository _skillRepository = Substitute.For<IPlayerSkillRepository>();
        private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public SP가_부족할_때()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _cache.GetSkillDataAsync(1).Returns(
                SkillData.Create(1, JobType.Warrior, true, 100, null, SkillEffectType.AtkFlat, 1.0));
            _skillRepository.FindByPlayerIdAndSkillIdAsync(Arg.Any<long>(), 1)
                .Returns((PlayerSkill?)null);

            var resource = PlayerResource.Create(1L);
            // Sp = 0 by default, spCost = 100 → insufficient
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(resource);
        }

        [Fact]
        public async Task BadRequestException이_발생한다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                skillRepo: _skillRepository, cache: _cache, userProvider: _currentUserProvider);

            await ((Func<Task>)(() => sut.ExecuteAsync(new SkillUnlockRequest(1)))).Should()
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
        private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
        private readonly IAppDbTransactionRunner _txRunner = Substitute.For<IAppDbTransactionRunner>();
        private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

        public 정상_요청시()
        {
            _currentUserProvider.GetAccountId().Returns(1L);
            _playerRepository.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
            _cache.GetSkillDataAsync(1).Returns(
                SkillData.Create(1, JobType.Warrior, true, 50, null, SkillEffectType.AtkFlat, 1.0));
            _skillRepository.FindByPlayerIdAndSkillIdAsync(Arg.Any<long>(), 1)
                .Returns((PlayerSkill?)null);

            var resource = PlayerResource.Create(1L);
            resource.UpdateChangeData(null, null, 100L);
            _resourceRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(resource);
            _stageRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
            _sessionRepository.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
            _weaponRepository.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
            _skillRepository.FindAllByPlayerIdAsync(Arg.Any<long>())
                .Returns([PlayerSkill.Create(1L, 1, true)]);
            _txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
                .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        }

        [Fact]
        public async Task WasAlreadyUnlocked가_false로_반환된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                redisRepo: _redisRepository, cache: _cache, txRunner: _txRunner,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(new SkillUnlockRequest(1));

            result.WasAlreadyUnlocked.Should().BeFalse();
            result.Changes.UnlockedSkillIds.Should().Contain(1);
        }

        [Fact]
        public async Task SP_소모량이_Changes에_반영된다()
        {
            var sut = BuildSut(playerRepo: _playerRepository, resourceRepo: _resourceRepository,
                stageRepo: _stageRepository, sessionRepo: _sessionRepository,
                weaponRepo: _weaponRepository, skillRepo: _skillRepository,
                redisRepo: _redisRepository, cache: _cache, txRunner: _txRunner,
                userProvider: _currentUserProvider);

            var result = await sut.ExecuteAsync(new SkillUnlockRequest(1));

            result.Changes.Sp.Should().Be(-50L);
        }
    }
}
