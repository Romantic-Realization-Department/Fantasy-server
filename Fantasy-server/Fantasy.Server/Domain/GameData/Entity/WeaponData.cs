using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.GameData.Entity;

public class WeaponData
{
    public int WeaponId { get; init; }
    public string Name { get; init; } = string.Empty;
    public WeaponGrade Grade { get; init; }
    public JobType JobType { get; init; }
    public long BaseAtk { get; init; }
    public long AtkPerEnhancement { get; init; }

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
