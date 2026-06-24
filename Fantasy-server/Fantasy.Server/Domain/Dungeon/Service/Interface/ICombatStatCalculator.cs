using Fantasy.Server.Domain.GameData.Entity;

namespace Fantasy.Server.Domain.Dungeon.Service.Interface;

public interface ICombatStatCalculator
{
    CombatStat Calculate(
        long level,
        JobBaseStat jobStat,
        WeaponData? weapon,
        long weaponEnhancementLevel,
        IEnumerable<(SkillData Skill, bool IsPassive)> unlockedSkills);

    double CalculateDps(CombatStat stat);
}
