namespace Fantasy.Server.Domain.Player.Entity;

public class PlayerWeapon
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public int WeaponId { get; private set; }
    public long Count { get; private set; }
    public long EnhancementLevel { get; private set; }
    public long AwakeningCount { get; private set; }

    public static PlayerWeapon Create(long playerId, int weaponId, long count, long enhancementLevel, long awakeningCount) => new()
    {
        PlayerId = playerId,
        WeaponId = weaponId,
        Count = count,
        EnhancementLevel = enhancementLevel,
        AwakeningCount = awakeningCount
    };

    public void Update(long count, long enhancementLevel, long awakeningCount)
    {
        Count = count;
        EnhancementLevel = enhancementLevel;
        AwakeningCount = awakeningCount;
    }
}
