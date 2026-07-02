namespace Fantasy.Server.Domain.GameData.Entity;

public class WeaponEnhancementCost
{
    public int WeaponId { get; private set; }
    public long EnhancementLevel { get; private set; } // 이 레벨 → 다음 레벨 비용
    public long RequiredGold { get; private set; }
    public long RequiredScroll { get; private set; }

    public static WeaponEnhancementCost Create(int weaponId, long enhancementLevel, long requiredGold, long requiredScroll) => new()
    {
        WeaponId = weaponId,
        EnhancementLevel = enhancementLevel,
        RequiredGold = requiredGold,
        RequiredScroll = requiredScroll
    };
}
