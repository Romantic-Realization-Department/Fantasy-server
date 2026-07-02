using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Constant;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Weapon.Service;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using FluentAssertions;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using NSubstitute;
using Xunit;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Test.Weapon.Service;

public class UpgradeWeaponServiceTest
{
    private static UpgradeWeaponService BuildSut(
        IPlayerRepository? playerRepo = null,
        IPlayerResourceRepository? resourceRepo = null,
        IPlayerStageRepository? stageRepo = null,
        IPlayerSessionRepository? sessionRepo = null,
        IPlayerWeaponRepository? weaponRepo = null,
        IPlayerSkillRepository? skillRepo = null,
        IPlayerRedisRepository? redisRepo = null,
        IRewardTransactionRepository? rewardTxRepo = null,
        IGameDataCacheService? cache = null,
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
        rewardTxRepo ??= Substitute.For<IRewardTransactionRepository>();
        cache ??= Substitute.For<IGameDataCacheService>();
        txRunner ??= Substitute.For<IAppDbTransactionRunner>();
        userProvider ??= Substitute.For<ICurrentUserProvider>();

        return new UpgradeWeaponService(
            playerRepo, resourceRepo, stageRepo, sessionRepo,
            weaponRepo, skillRepo, redisRepo, rewardTxRepo, cache, txRunner, userProvider);
    }

    private static (IPlayerRepository, IPlayerResourceRepository, IPlayerStageRepository,
        IPlayerSessionRepository, IPlayerWeaponRepository, IPlayerSkillRepository,
        IGameDataCacheService, ICurrentUserProvider) BuildHappyPathMocks(
        PlayerResource resource, PlayerWeapon playerWeapon)
    {
        var playerRepo = Substitute.For<IPlayerRepository>();
        var resourceRepo = Substitute.For<IPlayerResourceRepository>();
        var stageRepo = Substitute.For<IPlayerStageRepository>();
        var sessionRepo = Substitute.For<IPlayerSessionRepository>();
        var weaponRepo = Substitute.For<IPlayerWeaponRepository>();
        var skillRepo = Substitute.For<IPlayerSkillRepository>();
        var cache = Substitute.For<IGameDataCacheService>();
        var userProvider = Substitute.For<ICurrentUserProvider>();

        userProvider.GetAccountId().Returns(1L);
        playerRepo.FindByAccountAsync(1L).Returns(PlayerEntity.Create(1L, JobType.Warrior));
        resourceRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(resource);
        stageRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
        sessionRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
        weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1001).Returns(playerWeapon);
        weaponRepo.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([playerWeapon]);
        skillRepo.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
        cache.GetWeaponDataAsync(1001).Returns(WeaponData.Create(
            1001, "Rusty Sword", WeaponGrade.C, JobType.Warrior, 30, 5,
            maxEnhancementLevel: 10, maxAwakeningLevel: 3));
        cache.GetWeaponEnhancementCostAsync(1001, 0).Returns(WeaponEnhancementCost.Create(1001, 0, 100, 0));

        return (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider);
    }

    [Fact]
    public async Task ExecuteAsync_플레이어가_없으면_NotFoundException이_발생한다()
    {
        var userProvider = Substitute.For<ICurrentUserProvider>();
        var playerRepo = Substitute.For<IPlayerRepository>();
        userProvider.GetAccountId().Returns(1L);
        playerRepo.FindByAccountAsync(1L).Returns((PlayerEntity?)null);
        var sut = BuildSut(playerRepo: playerRepo, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1001);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_미보유_무기면_NotFoundException이_발생한다()
    {
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(PlayerResource.Create(1L), PlayerWeapon.Create(1L, 1001, 1L, 0L, 0L));
        weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1001).Returns((PlayerWeapon?)null);
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache: cache, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1001);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_최대_강화_레벨이면_BadRequestException이_발생한다()
    {
        var weapon = PlayerWeapon.Create(1L, 1001, 1L, 10L, 0L);
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(PlayerResource.Create(1L), weapon);
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache: cache, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1001);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_재화가_부족하면_BadRequestException이_발생한다()
    {
        // PlayerResource.Create는 Gold 0으로 시작 — 비용 100을 못 냄
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(PlayerResource.Create(1L), PlayerWeapon.Create(1L, 1001, 1L, 0L, 0L));
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache: cache, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1001);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_성공하면_골드가_차감되고_강화_레벨이_오른다()
    {
        var resource = PlayerResource.Create(1L);
        resource.UpdateGold(500L);
        var weapon = PlayerWeapon.Create(1L, 1001, 1L, 0L, 0L);
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(resource, weapon);
        var txRunner = Substitute.For<IAppDbTransactionRunner>();
        txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        var redisRepo = Substitute.For<IPlayerRedisRepository>();
        var rewardTxRepo = Substitute.For<IRewardTransactionRepository>();
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo,
            redisRepo: redisRepo, rewardTxRepo: rewardTxRepo, cache: cache, txRunner: txRunner, userProvider: userProvider);

        var result = await sut.ExecuteAsync(1001);

        result.EnhancementLevel.Should().Be(1L);
        result.Changes.Gold.Should().Be(-100L);
        resource.Gold.Should().Be(400L);
        weapon.EnhancementLevel.Should().Be(1L);
        await resourceRepo.Received(1).UpdateAsync(resource);
        await weaponRepo.Received(1).UpdateAsync(weapon);
        await redisRepo.Received(1).DeleteAsync(1L);
        await rewardTxRepo.Received(1).SaveRangeAsync(
            Arg.Is<List<RewardTransaction>>(list =>
                list.Count == 1 &&
                list[0].SourceType == RewardSourceTypes.WeaponUpgrade &&
                list[0].RewardType == RewardTypes.Gold &&
                list[0].Amount == -100L));
    }
}
