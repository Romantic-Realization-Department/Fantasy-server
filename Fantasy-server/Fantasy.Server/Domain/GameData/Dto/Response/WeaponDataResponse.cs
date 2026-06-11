namespace Fantasy.Server.Domain.GameData.Dto.Response;

public record WeaponDataResponse(
    int WeaponId,
    string Name,
    string Grade,
    string JobType,
    long BaseAtk,
    long AtkPerEnhancement
);
