using Fantasy.Server.Domain.LevelUp.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record BossDungeonResponse(
    bool Cleared,
    long EarnedMithril,
    DroppedWeaponInfo? DroppedWeapon,
    long EarnedXp,
    List<LevelUpResult> LevelUps
);
