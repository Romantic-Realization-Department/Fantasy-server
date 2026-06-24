namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record WeaponDungeonResponse(
    bool Cleared,
    List<DroppedWeaponInfo> DroppedWeapons,
    long DroppedScrolls
);
