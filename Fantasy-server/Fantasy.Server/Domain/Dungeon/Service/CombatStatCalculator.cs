using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;

namespace Fantasy.Server.Domain.Dungeon.Service;

public record CombatStat(long Atk, long Hp, double CritRate, double CritDmgMultiplier);

public class CombatStatCalculator
{
    public CombatStat Calculate(
        long level,
        JobBaseStat jobStat,
        WeaponData? weapon,
        long weaponEnhancementLevel,
        IEnumerable<(SkillData Skill, bool IsPassive)> unlockedSkills)
    {
        var atk = (long)(jobStat.BaseAtk + jobStat.AtkPerLevel * (level - 1));
        var hp = (long)(jobStat.BaseHp + jobStat.HpPerLevel * (level - 1));
        var critRate = jobStat.CritRate;
        var critDmg = jobStat.CritDmgMultiplier;

        if (weapon is not null)
            atk += weapon.BaseAtk + weapon.AtkPerEnhancement * weaponEnhancementLevel;

        foreach (var (skill, _) in unlockedSkills)
        {
            switch (skill.EffectType)
            {
                case SkillEffectType.AtkFlat:
                    atk += (long)skill.EffectValue;
                    break;
                case SkillEffectType.AtkPercent:
                    atk = (long)(atk * (1 + skill.EffectValue / 100.0));
                    break;
                case SkillEffectType.HpFlat:
                    hp += (long)skill.EffectValue;
                    break;
                case SkillEffectType.HpPercent:
                    hp = (long)(hp * (1 + skill.EffectValue / 100.0));
                    break;
                case SkillEffectType.CritRate:
                    critRate += skill.EffectValue;
                    break;
                case SkillEffectType.CritDmg:
                    critDmg += skill.EffectValue;
                    break;
            }
        }

        return new CombatStat(atk, hp, critRate, critDmg);
    }

    public double CalculateDps(CombatStat stat)
        => stat.Atk * (1 + stat.CritRate * (stat.CritDmgMultiplier - 1));
}
