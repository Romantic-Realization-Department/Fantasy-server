namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record ChangesDto(
    long Gold,
    long Exp,
    long Sp,
    long Mithril,
    long EnhancementScroll,
    int DungeonTickets,
    List<long> LevelUps,
    List<int> UnlockedSkillIds,
    List<int> AcquiredWeaponIds,
    long MaxStage
)
{
    public static ChangesDto Empty() => new(0, 0, 0, 0, 0, 0, [], [], [], 0);
}
