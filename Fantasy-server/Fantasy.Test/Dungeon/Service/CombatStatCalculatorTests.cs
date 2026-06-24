using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.Player.Enum;
using FluentAssertions;
using Xunit;

namespace Fantasy.Test.Dungeon.Service;

public class CombatStatCalculatorTests
{
    private readonly CombatStatCalculator _sut = new();

    private static JobBaseStat MakeJobStat() =>
        JobBaseStat.Create(JobType.Warrior, baseHp: 1000, baseAtk: 100, critRate: 0.1, critDmgMultiplier: 1.5, hpPerLevel: 50, atkPerLevel: 10);

    public class 무기와_스킬이_없을_때
    {
        private readonly CombatStatCalculator _sut = new();

        [Fact]
        public void 직업_기본_스탯으로_계산된다()
        {
            var jobStat = MakeJobStat();

            var result = _sut.Calculate(1, jobStat, null, 0, []);

            result.Atk.Should().Be(jobStat.BaseAtk);
            result.Hp.Should().Be(jobStat.BaseHp);
            result.CritRate.Should().Be(jobStat.CritRate);
            result.CritDmgMultiplier.Should().Be(jobStat.CritDmgMultiplier);
        }

        [Fact]
        public void 레벨이_높을수록_스탯이_증가한다()
        {
            var jobStat = MakeJobStat();

            var lv1 = _sut.Calculate(1, jobStat, null, 0, []);
            var lv10 = _sut.Calculate(10, jobStat, null, 0, []);

            lv10.Atk.Should().BeGreaterThan(lv1.Atk);
            lv10.Hp.Should().BeGreaterThan(lv1.Hp);
        }

        private static JobBaseStat MakeJobStat() =>
            JobBaseStat.Create(JobType.Warrior, 1000, 100, 0.1, 1.5, 50, 10);
    }

    public class 무기를_장착했을_때
    {
        private readonly CombatStatCalculator _sut = new();
        private readonly JobBaseStat _jobStat = JobBaseStat.Create(JobType.Warrior, 1000, 100, 0.1, 1.5, 50, 10);

        [Fact]
        public void 무기_기본_공격력이_합산된다()
        {
            var weapon = WeaponData.Create(1, "검", WeaponGrade.C, JobType.Warrior, baseAtk: 200, atkPerEnhancement: 10);

            var result = _sut.Calculate(1, _jobStat, weapon, 0, []);

            result.Atk.Should().Be(_jobStat.BaseAtk + weapon.BaseAtk);
        }

        [Fact]
        public void 강화_레벨에_비례해_공격력이_증가한다()
        {
            var weapon = WeaponData.Create(1, "검", WeaponGrade.C, JobType.Warrior, baseAtk: 200, atkPerEnhancement: 10);

            var lv0 = _sut.Calculate(1, _jobStat, weapon, 0, []);
            var lv5 = _sut.Calculate(1, _jobStat, weapon, 5, []);

            lv5.Atk.Should().Be(lv0.Atk + weapon.AtkPerEnhancement * 5);
        }
    }

    public class 패시브_스킬이_있을_때
    {
        private readonly CombatStatCalculator _sut = new();
        private readonly JobBaseStat _jobStat = JobBaseStat.Create(JobType.Warrior, 1000, 100, 0.1, 1.5, 50, 10);

        [Fact]
        public void AtkFlat_스킬이_공격력에_더해진다()
        {
            var skill = SkillData.Create(1, JobType.Warrior, isActive: false, spCost: 2,
                prereqSkillId: null, effectType: SkillEffectType.AtkFlat, effectValue: 50);

            var result = _sut.Calculate(1, _jobStat, null, 0,
                [(Skill: skill, IsPassive: true)]);

            result.Atk.Should().Be(_jobStat.BaseAtk + 50);
        }

        [Fact]
        public void CritRate_스킬이_크리티컬_확률에_더해진다()
        {
            var skill = SkillData.Create(1, JobType.Warrior, isActive: false, spCost: 2,
                prereqSkillId: null, effectType: SkillEffectType.CritRate, effectValue: 0.1);

            var result = _sut.Calculate(1, _jobStat, null, 0,
                [(Skill: skill, IsPassive: true)]);

            result.CritRate.Should().BeApproximately(_jobStat.CritRate + 0.1, 1e-9);
        }
    }

    public class DPS_계산
    {
        private readonly CombatStatCalculator _sut = new();

        [Fact]
        public void 크리티컬이_없을_때_DPS는_공격력과_같다()
        {
            var stat = new CombatStat(Atk: 100, Hp: 1000, CritRate: 0, CritDmgMultiplier: 1.5);

            var dps = _sut.CalculateDps(stat);

            dps.Should().BeApproximately(100, 1e-9);
        }

        [Fact]
        public void 크리티컬이_있을_때_DPS가_증가한다()
        {
            var stat = new CombatStat(Atk: 100, Hp: 1000, CritRate: 1.0, CritDmgMultiplier: 2.0);

            var dps = _sut.CalculateDps(stat);

            dps.Should().BeApproximately(200, 1e-9);
        }
    }
}
