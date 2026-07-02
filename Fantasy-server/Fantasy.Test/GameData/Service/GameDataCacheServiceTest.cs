using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Repository.Interface;
using Fantasy.Server.Domain.GameData.Service;
using Fantasy.Server.Domain.Player.Enum;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using Xunit;

namespace Fantasy.Test.GameData.Service;

public class GameDataCacheServiceTest
{
    private static GameDataCacheService BuildSut(
        IGameDataRepository? repository = null,
        IDistributedCache? cache = null) =>
        new(
            repository ?? Substitute.For<IGameDataRepository>(),
            cache ?? Substitute.For<IDistributedCache>());

    public class 캐시_미스_시
    {
        private readonly IGameDataRepository _repository = Substitute.For<IGameDataRepository>();
        private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
        private readonly GameDataCacheService _sut;

        public 캐시_미스_시()
        {
            _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
            _sut = BuildSut(_repository, _cache);
        }

        [Fact]
        public async Task GetLevelTableAsync_DB에서_데이터를_조회한다()
        {
            _repository.GetAllLevelTablesAsync().Returns([
                LevelTable.Create(1, 100, 3),
                LevelTable.Create(2, 200, 5)
            ]);

            var result = await _sut.GetLevelTableAsync();

            result.Should().HaveCount(2);
            result.Should().ContainKey(1);
            result.Should().ContainKey(2);
        }

        [Fact]
        public async Task GetWeaponDataByGradeAsync_등급별_무기를_반환한다()
        {
            _repository.GetAllWeaponDatasAsync().Returns([
                WeaponData.Create(1, "Iron Sword", WeaponGrade.C, JobType.Warrior, 100, 10),
                WeaponData.Create(2, "Steel Bow", WeaponGrade.B, JobType.Archer, 120, 12)
            ]);

            var result = await _sut.GetWeaponDataByGradeAsync(WeaponGrade.C);

            result.Should().HaveCount(1);
            result[0].Grade.Should().Be(WeaponGrade.C);
        }

        [Fact]
        public async Task GetWeaponDataByJobAsync_직업별_무기를_반환한다()
        {
            _repository.GetAllWeaponDatasAsync().Returns([
                WeaponData.Create(1, "Iron Sword", WeaponGrade.C, JobType.Warrior, 100, 10),
                WeaponData.Create(2, "Steel Bow", WeaponGrade.B, JobType.Archer, 120, 12)
            ]);

            var result = await _sut.GetWeaponDataByJobAsync(JobType.Warrior);

            result.Should().HaveCount(1);
            result[0].JobType.Should().Be(JobType.Warrior);
        }

        [Fact]
        public async Task GetWeaponDataAsync_무기_ID로_단건_반환한다()
        {
            _repository.GetAllWeaponDatasAsync().Returns([
                WeaponData.Create(1, "Iron Sword", WeaponGrade.C, JobType.Warrior, 100, 10),
                WeaponData.Create(2, "Steel Bow", WeaponGrade.B, JobType.Archer, 120, 12)
            ]);

            var result = await _sut.GetWeaponDataAsync(1);

            result.Should().NotBeNull();
            result!.WeaponId.Should().Be(1);
        }

        [Fact]
        public async Task GetWeaponDataAsync_존재하지_않는_ID면_null을_반환한다()
        {
            _repository.GetAllWeaponDatasAsync().Returns([]);

            var result = await _sut.GetWeaponDataAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSkillDataByJobAsync_직업별_스킬을_반환한다()
        {
            _repository.GetAllSkillDatasAsync().Returns([
                SkillData.Create(1, JobType.Warrior, true, 10, null, SkillEffectType.AtkFlat, 50.0),
                SkillData.Create(2, JobType.Archer, false, 5, null, SkillEffectType.CritRate, 0.1)
            ]);

            var result = await _sut.GetSkillDataByJobAsync(JobType.Warrior);

            result.Should().HaveCount(1);
            result[0].JobType.Should().Be(JobType.Warrior);
        }

        [Fact]
        public async Task GetSkillDataAsync_스킬_ID로_단건_반환한다()
        {
            _repository.GetAllSkillDatasAsync().Returns([
                SkillData.Create(1, JobType.Warrior, true, 10, null, SkillEffectType.AtkFlat, 50.0)
            ]);

            var result = await _sut.GetSkillDataAsync(1);

            result.Should().NotBeNull();
            result!.SkillId.Should().Be(1);
        }

        [Fact]
        public async Task GetStageDataAsync_스테이지_번호로_단건_반환한다()
        {
            _repository.GetAllStageDatasAsync().Returns([
                StageData.Create(1, 1000, 100, 10, 20),
                StageData.Create(2, 2000, 200, 15, 30)
            ]);

            var result = await _sut.GetStageDataAsync(1);

            result.Should().NotBeNull();
            result!.Stage.Should().Be(1);
        }

        [Fact]
        public async Task GetJobBaseStatAsync_직업별_기본_스탯을_반환한다()
        {
            _repository.GetAllJobBaseStatsAsync().Returns([
                JobBaseStat.Create(JobType.Warrior, 1000, 100, 0.05, 1.5, 50.0, 10.0),
                JobBaseStat.Create(JobType.Archer, 800, 120, 0.10, 1.8, 40.0, 12.0)
            ]);

            var result = await _sut.GetJobBaseStatAsync(JobType.Warrior);

            result.Should().NotBeNull();
            result!.JobType.Should().Be(JobType.Warrior);
        }

        [Fact]
        public async Task GetWeaponEnhancementCostAsync_캐시_미스면_리포지토리에서_읽고_해당_레벨_비용을_반환한다()
        {
            _repository.GetAllWeaponEnhancementCostsAsync().Returns([
                WeaponEnhancementCost.Create(1001, 0, 100, 0),
                WeaponEnhancementCost.Create(1001, 5, 600, 1)
            ]);

            var result = await _sut.GetWeaponEnhancementCostAsync(1001, 5);

            result.Should().NotBeNull();
            result!.RequiredGold.Should().Be(600);
            result.RequiredScroll.Should().Be(1);
        }

        [Fact]
        public async Task GetWeaponAwakenCostAsync_없는_레벨이면_null을_반환한다()
        {
            _repository.GetAllWeaponAwakenCostsAsync().Returns([
                WeaponAwakenCost.Create(1001, 0, 1, 5)
            ]);

            var result = await _sut.GetWeaponAwakenCostAsync(1001, 3);

            result.Should().BeNull();
        }
    }

    public class 캐시_히트_시
    {
        private readonly IGameDataRepository _repository = Substitute.For<IGameDataRepository>();
        private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
        private readonly GameDataCacheService _sut;

        public 캐시_히트_시()
        {
            var levelTableJson = System.Text.Json.JsonSerializer.Serialize(new Dictionary<long, LevelTable>
            {
                [1] = LevelTable.Create(1, 100, 3)
            });
            _cache.GetAsync("game_data:level_table", Arg.Any<CancellationToken>())
                .Returns(System.Text.Encoding.UTF8.GetBytes(levelTableJson));
            _sut = BuildSut(_repository, _cache);
        }

        [Fact]
        public async Task GetLevelTableAsync_DB를_조회하지_않는다()
        {
            var result = await _sut.GetLevelTableAsync();

            result.Should().HaveCount(1);
            await _repository.DidNotReceive().GetAllLevelTablesAsync();
        }

        [Fact]
        public async Task GetLevelTableAsync_역직렬화된_Level과_RequiredExp_값을_반환한다()
        {
            var result = await _sut.GetLevelTableAsync();

            result[1].Level.Should().Be(1);
            result[1].RequiredExp.Should().Be(100);
        }

        [Fact]
        public async Task GetWeaponDataAsync_역직렬화된_값을_반환한다()
        {
            var weaponDataJson = System.Text.Json.JsonSerializer.Serialize(new List<WeaponData>
            {
                WeaponData.Create(1, "Iron Sword", WeaponGrade.C, JobType.Warrior, 100, 10, 15, 5, 3, 2)
            });
            _cache.GetAsync("game_data:weapon_data:v2", Arg.Any<CancellationToken>())
                .Returns(System.Text.Encoding.UTF8.GetBytes(weaponDataJson));

            var result = await _sut.GetWeaponDataAsync(1);

            result.Should().NotBeNull();
            result!.WeaponId.Should().Be(1);
            result.Name.Should().Be("Iron Sword");
            result.MaxEnhancementLevel.Should().Be(15);
            result.SynthesizeRequiredCount.Should().Be(3);
        }

        [Fact]
        public async Task GetStageDataAsync_역직렬화된_값을_반환한다()
        {
            var stageDataJson = System.Text.Json.JsonSerializer.Serialize(new List<StageData>
            {
                StageData.Create(7, 5000, 300, 25, 40, true)
            });
            _cache.GetAsync("game_data:stage_data", Arg.Any<CancellationToken>())
                .Returns(System.Text.Encoding.UTF8.GetBytes(stageDataJson));

            var result = await _sut.GetStageDataAsync(7);

            result.Should().NotBeNull();
            result!.Stage.Should().Be(7);
            result.MonsterHp.Should().Be(5000);
        }

        [Fact]
        public async Task GetWeaponEnhancementCostAsync_역직렬화된_값을_반환한다()
        {
            var costJson = System.Text.Json.JsonSerializer.Serialize(new List<WeaponEnhancementCost>
            {
                WeaponEnhancementCost.Create(1001, 5, 600, 1)
            });
            _cache.GetAsync("game_data:weapon_enhancement_cost", Arg.Any<CancellationToken>())
                .Returns(System.Text.Encoding.UTF8.GetBytes(costJson));

            var result = await _sut.GetWeaponEnhancementCostAsync(1001, 5);

            result.Should().NotBeNull();
            result!.RequiredGold.Should().Be(600);
            result.RequiredScroll.Should().Be(1);
        }

        [Fact]
        public async Task GetWeaponAwakenCostAsync_역직렬화된_값을_반환한다()
        {
            var costJson = System.Text.Json.JsonSerializer.Serialize(new List<WeaponAwakenCost>
            {
                WeaponAwakenCost.Create(1001, 3, 2, 8)
            });
            _cache.GetAsync("game_data:weapon_awaken_cost", Arg.Any<CancellationToken>())
                .Returns(System.Text.Encoding.UTF8.GetBytes(costJson));

            var result = await _sut.GetWeaponAwakenCostAsync(1001, 3);

            result.Should().NotBeNull();
            result!.RequiredCount.Should().Be(2);
            result.RequiredMithril.Should().Be(8);
        }

        [Fact]
        public async Task GetJobBaseStatAsync_역직렬화된_값을_반환한다()
        {
            var jobBaseStatJson = System.Text.Json.JsonSerializer.Serialize(new List<JobBaseStat>
            {
                JobBaseStat.Create(JobType.Archer, 800, 120, 0.10, 1.8, 40.0, 12.0)
            });
            _cache.GetAsync("game_data:job_base_stat", Arg.Any<CancellationToken>())
                .Returns(System.Text.Encoding.UTF8.GetBytes(jobBaseStatJson));

            var result = await _sut.GetJobBaseStatAsync(JobType.Archer);

            result.Should().NotBeNull();
            result!.JobType.Should().Be(JobType.Archer);
            result.BaseHp.Should().Be(800);
        }

        [Fact]
        public async Task GetSkillDataAsync_역직렬화된_값을_반환한다()
        {
            var skillDataJson = System.Text.Json.JsonSerializer.Serialize(new List<SkillData>
            {
                SkillData.Create(1, JobType.Warrior, true, 10, null, SkillEffectType.AtkFlat, 50.0)
            });
            _cache.GetAsync("game_data:skill_data", Arg.Any<CancellationToken>())
                .Returns(System.Text.Encoding.UTF8.GetBytes(skillDataJson));

            var result = await _sut.GetSkillDataAsync(1);

            result.Should().NotBeNull();
            result!.SkillId.Should().Be(1);
            result.EffectValue.Should().Be(50.0);
        }
    }
}
