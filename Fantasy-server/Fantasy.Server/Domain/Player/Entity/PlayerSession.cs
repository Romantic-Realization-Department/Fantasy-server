namespace Fantasy.Server.Domain.Player.Entity;

public class PlayerSession
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public int? LastWeaponId { get; private set; }
    public int[] ActiveSkills { get; private set; } = [];
    public DateTime UpdatedAt { get; private set; }

    public static PlayerSession Create(long playerId) => new()
    {
        PlayerId = playerId,
        LastWeaponId = null,
        ActiveSkills = [],
        UpdatedAt = DateTime.UtcNow
    };

    public void Update(int lastWeaponId, int[] activeSkills)
    {
        LastWeaponId = lastWeaponId;
        ActiveSkills = activeSkills;
        UpdatedAt = DateTime.UtcNow;
    }
}
