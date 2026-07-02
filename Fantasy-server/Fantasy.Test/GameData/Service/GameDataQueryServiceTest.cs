using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Service;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Enum;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Fantasy.Test.GameData.Service;

public class GameDataQueryServiceTest
{
    private readonly IGameDataCacheService _cache = Substitute.For<IGameDataCacheService>();
    private readonly GameDataQueryService _sut;

    public GameDataQueryServiceTest()
    {
        _sut = new GameDataQueryService(_cache);
    }

    [Fact]
    public async Task GetSkillsByJobAsync_직업별_스킬_응답을_반환한다()
    {
        // Arrange
        _cache.GetSkillDataByJobAsync(JobType.Warrior).Returns([
            SkillData.Create(1, JobType.Warrior, true, 10, null, SkillEffectType.AtkFlat, 50.0),
            SkillData.Create(2, JobType.Warrior, false, 5, 1, SkillEffectType.AtkPercent, 0.2)
        ]);

        // Act
        var result = await _sut.GetSkillsByJobAsync(JobType.Warrior);

        // Assert
        result.Should().HaveCount(2);
        result[0].SkillId.Should().Be(1);
        result[0].JobType.Should().Be("Warrior");
        result[1].PrereqSkillId.Should().Be(1);
    }

    [Fact]
    public async Task GetWeaponsByJobAsync_직업별_무기_응답을_반환한다()
    {
        // Arrange
        _cache.GetWeaponDataByJobAsync(JobType.Archer).Returns([
            WeaponData.Create(1, "Iron Bow", WeaponGrade.C, JobType.Archer, 100, 10,
                maxEnhancementLevel: 10, maxAwakeningLevel: 3, synthesizeRequiredCount: 3, synthesizeResultWeaponId: 2),
            WeaponData.Create(2, "Steel Bow", WeaponGrade.B, JobType.Archer, 150, 15,
                maxEnhancementLevel: 10, maxAwakeningLevel: 3)
        ]);

        // Act
        var result = await _sut.GetWeaponsByJobAsync(JobType.Archer);

        // Assert
        result.Should().HaveCount(2);
        result[0].WeaponId.Should().Be(1);
        result[0].Name.Should().Be("Iron Bow");
        result[0].Grade.Should().Be("C");
        result[0].JobType.Should().Be("Archer");
        result[0].MaxEnhancementLevel.Should().Be(10);
        result[0].SynthesizeRequiredCount.Should().Be(3);
        result[0].SynthesizeResultWeaponId.Should().Be(2);
        result[1].SynthesizeRequiredCount.Should().BeNull();
    }

    [Fact]
    public async Task GetLevelsAsync_레벨_목록을_오름차순으로_반환한다()
    {
        // Arrange
        _cache.GetLevelTableAsync().Returns(new Dictionary<long, LevelTable>
        {
            [2] = LevelTable.Create(2, 200, 5),
            [1] = LevelTable.Create(1, 100, 3),
            [3] = LevelTable.Create(3, 400, 7)
        });

        // Act
        var result = await _sut.GetLevelsAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Level.Should().Be(1);
        result[1].Level.Should().Be(2);
        result[2].Level.Should().Be(3);
    }

    [Fact]
    public async Task GetStagesAsync_스테이지_목록을_반환한다()
    {
        // Arrange
        _cache.GetAllStageDataAsync().Returns([
            StageData.Create(1, 1000, 100, 10, 20),
            StageData.Create(2, 2000, 200, 15, 30)
        ]);

        // Act
        var result = await _sut.GetStagesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Stage.Should().Be(1);
        result[0].MonsterHp.Should().Be(1000);
        result[1].Stage.Should().Be(2);
    }
}
