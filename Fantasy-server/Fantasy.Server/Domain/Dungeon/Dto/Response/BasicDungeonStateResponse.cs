namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record BasicDungeonStateResponse(
    long Stage,
    DateTime LastCalculatedAt,
    DateTime ServerNow,
    int MaxOfflineSeconds,
    double CombatPower,
    long GoldPerSecond,
    long XpPerSecond
);
