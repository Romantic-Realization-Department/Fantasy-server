namespace Fantasy.Server.Domain.GameData.Entity;

public class WeaponAwakenCost
{
    public int WeaponId { get; private set; }
    public long AwakeningLevel { get; private set; } // 이 레벨 → 다음 레벨 비용
    public int RequiredCount { get; private set; }   // 자신 제외 소모 복사본 수
    public int RequiredMithril { get; private set; }

    public static WeaponAwakenCost Create(int weaponId, long awakeningLevel, int requiredCount, int requiredMithril) => new()
    {
        WeaponId = weaponId,
        AwakeningLevel = awakeningLevel,
        RequiredCount = requiredCount,
        RequiredMithril = requiredMithril
    };
}
