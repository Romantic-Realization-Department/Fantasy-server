namespace Fantasy.Server.Domain.Player.Entity;

public class PlayerSkill
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public int SkillId { get; private set; }
    public bool IsUnlocked { get; private set; }

    public static PlayerSkill Create(long playerId, int skillId, bool isUnlocked) => new()
    {
        PlayerId = playerId,
        SkillId = skillId,
        IsUnlocked = isUnlocked
    };

    public void Update(bool isUnlocked)
    {
        IsUnlocked = isUnlocked;
    }
}
