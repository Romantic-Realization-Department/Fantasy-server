using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.GameData.Entity;

public class WeaponData
{
    public int WeaponId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public WeaponGrade Grade { get; private set; }
    public JobType JobType { get; private set; }
    public long BaseAtk { get; private set; }
    public long AtkPerEnhancement { get; private set; }

    public static WeaponData Create(
        int weaponId,
        string name,
        WeaponGrade grade,
        JobType jobType,
        long baseAtk,
        long atkPerEnhancement) => new()
    {
        WeaponId = weaponId,
        Name = name,
        Grade = grade,
        JobType = jobType,
        BaseAtk = baseAtk,
        AtkPerEnhancement = atkPerEnhancement
    };
}
