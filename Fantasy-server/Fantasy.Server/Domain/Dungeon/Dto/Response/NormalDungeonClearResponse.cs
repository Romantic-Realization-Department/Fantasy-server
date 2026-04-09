using Fantasy.Server.Domain.LevelUp.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record NormalDungeonClearResponse(
    long EarnedGold,
    long EarnedXp,
    long NewMaxStage,
    long NewLevel,
    List<LevelUpResult> LevelUps
);
