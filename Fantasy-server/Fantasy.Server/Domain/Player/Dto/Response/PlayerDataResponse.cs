using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Dto.Response;

public record PlayerDataResponse(
    JobType JobType,
    long Level,
    long MaxStage,
    int? LastWeaponId,
    int[] ActiveSkills,
    long Gold,
    long Exp,
    long EnhancementScroll,
    long Mithril,
    long Sp,
    List<WeaponInfoResponse> Weapons,
    List<SkillInfoResponse> Skills
);

public record WeaponInfoResponse(int WeaponId, long Count, long EnhancementLevel, long AwakeningCount);

public record SkillInfoResponse(int SkillId, bool IsUnlocked);
