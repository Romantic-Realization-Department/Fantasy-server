namespace Fantasy.Server.Domain.GameData.Dto.Response;

public record SkillDataResponse(
    int SkillId,
    string JobType,
    bool IsActive,
    long SpCost,
    int? PrereqSkillId,
    string EffectType,
    double EffectValue
);
