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

public class SynthesizeWeaponServiceTest
{
    private static SynthesizeWeaponService BuildSut(
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

        return new SynthesizeWeaponService(
            playerRepo, resourceRepo, stageRepo, sessionRepo,
            weaponRepo, skillRepo, redisRepo, rewardTxRepo, cache, txRunner, userProvider);
    }

    private static (IPlayerRepository, IPlayerResourceRepository, IPlayerStageRepository,
        IPlayerSessionRepository, IPlayerWeaponRepository, IPlayerSkillRepository,
        IGameDataCacheService, ICurrentUserProvider) BuildHappyPathMocks(PlayerWeapon material)
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
        resourceRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerResource.Create(1L));
        stageRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerStage.Create(1L));
        sessionRepo.FindByPlayerIdAsync(Arg.Any<long>()).Returns(PlayerSession.Create(1L));
        weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1001).Returns(material);
        weaponRepo.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([material]);
        skillRepo.FindAllByPlayerIdAsync(Arg.Any<long>()).Returns([]);
        cache.GetWeaponDataAsync(1001).Returns(WeaponData.Create(
            1001, "Rusty Sword", WeaponGrade.C, JobType.Warrior, 30, 5,
            maxEnhancementLevel: 10, maxAwakeningLevel: 3,
            synthesizeRequiredCount: 3, synthesizeResultWeaponId: 1002));

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
    public async Task ExecuteAsync_합성_불가_무기면_BadRequestException이_발생한다()
    {
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(PlayerWeapon.Create(1L, 1001, 3L, 0L, 0L));
        cache.GetWeaponDataAsync(1002).Returns(WeaponData.Create(
            1002, "Iron Sword", WeaponGrade.B, JobType.Warrior, 50, 8,
            maxEnhancementLevel: 10, maxAwakeningLevel: 3));
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache: cache, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1002);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_미보유_재료면_NotFoundException이_발생한다()
    {
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(PlayerWeapon.Create(1L, 1001, 3L, 0L, 0L));
        weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1001).Returns((PlayerWeapon?)null);
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache: cache, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1001);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_재료가_부족하면_BadRequestException이_발생한다()
    {
        var material = PlayerWeapon.Create(1L, 1001, 2L, 0L, 0L);
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(material);
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache: cache, userProvider: userProvider);

        var act = async () => await sut.ExecuteAsync(1001);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_결과_무기가_없으면_새로_생성된다()
    {
        var material = PlayerWeapon.Create(1L, 1001, 3L, 5L, 1L); // 강화/각성돼 있어도 재료 허용 (가정 #3)
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(material);
        weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1002).Returns((PlayerWeapon?)null);
        var txRunner = Substitute.For<IAppDbTransactionRunner>();
        txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        var redisRepo = Substitute.For<IPlayerRedisRepository>();
        var rewardTxRepo = Substitute.For<IRewardTransactionRepository>();
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo,
            redisRepo: redisRepo, rewardTxRepo: rewardTxRepo, cache: cache, txRunner: txRunner, userProvider: userProvider);

        var result = await sut.ExecuteAsync(1001);

        material.Count.Should().Be(0L);
        result.AcquiredWeaponId.Should().Be(1002);
        result.Changes.AcquiredWeaponIds.Should().BeEquivalentTo([1002]);
        await weaponRepo.Received(1).SaveAsync(Arg.Is<PlayerWeapon>(w =>
            w.WeaponId == 1002 && w.Count == 1L && w.EnhancementLevel == 0L && w.AwakeningCount == 0L));
        await weaponRepo.Received(1).UpdateAsync(material);
        await redisRepo.Received(1).DeleteAsync(1L);
        await rewardTxRepo.Received(1).SaveRangeAsync(
            Arg.Is<List<RewardTransaction>>(list =>
                list.Any(t => t.SourceType == RewardSourceTypes.WeaponSynthesize
                    && t.RewardType == RewardTypes.Weapon && t.RewardRefId == "1001" && t.Amount == -3L) &&
                list.Any(t => t.SourceType == RewardSourceTypes.WeaponSynthesize
                    && t.RewardType == RewardTypes.Weapon && t.RewardRefId == "1002" && t.Amount == 1L)));
    }

    [Fact]
    public async Task ExecuteAsync_결과_무기를_이미_보유하면_Count가_1_증가한다()
    {
        var material = PlayerWeapon.Create(1L, 1001, 5L, 0L, 0L);
        var existing = PlayerWeapon.Create(1L, 1002, 2L, 4L, 0L);
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(material);
        weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1002).Returns(existing);
        var txRunner = Substitute.For<IAppDbTransactionRunner>();
        txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo,
            cache: cache, txRunner: txRunner, userProvider: userProvider);

        var result = await sut.ExecuteAsync(1001);

        material.Count.Should().Be(2L);
        existing.Count.Should().Be(3L);
        existing.EnhancementLevel.Should().Be(4L); // 기존 강화 레벨 유지
        await weaponRepo.Received(1).UpdateAsync(existing);
    }

    [Fact]
    public async Task ExecuteAsync_합성_필요_수량이_0이면_결과_행만_기록된다()
    {
        var material = PlayerWeapon.Create(1L, 1001, 3L, 0L, 0L);
        var (playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo, cache, userProvider) =
            BuildHappyPathMocks(material);
        cache.GetWeaponDataAsync(1001).Returns(WeaponData.Create(
            1001, "Rusty Sword", WeaponGrade.C, JobType.Warrior, 30, 5,
            maxEnhancementLevel: 10, maxAwakeningLevel: 3,
            synthesizeRequiredCount: 0, synthesizeResultWeaponId: 1002));
        weaponRepo.FindByPlayerIdAndWeaponIdAsync(Arg.Any<long>(), 1002).Returns((PlayerWeapon?)null);
        var txRunner = Substitute.For<IAppDbTransactionRunner>();
        txRunner.ExecuteAsync(Arg.Any<Func<Task>>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>()());
        var redisRepo = Substitute.For<IPlayerRedisRepository>();
        var rewardTxRepo = Substitute.For<IRewardTransactionRepository>();
        var sut = BuildSut(playerRepo, resourceRepo, stageRepo, sessionRepo, weaponRepo, skillRepo,
            redisRepo: redisRepo, rewardTxRepo: rewardTxRepo, cache: cache, txRunner: txRunner, userProvider: userProvider);

        var result = await sut.ExecuteAsync(1001);

        result.AcquiredWeaponId.Should().Be(1002);
        await rewardTxRepo.Received(1).SaveRangeAsync(
            Arg.Is<List<RewardTransaction>>(list =>
                list.Count == 1 &&
                list[0].SourceType == RewardSourceTypes.WeaponSynthesize &&
                list[0].RewardType == RewardTypes.Weapon &&
                list[0].RewardRefId == "1002" &&
                list[0].Amount == 1L));
    }
}
