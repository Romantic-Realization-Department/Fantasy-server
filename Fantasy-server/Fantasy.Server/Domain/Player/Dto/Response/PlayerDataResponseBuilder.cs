using Fantasy.Server.Domain.Player.Entity;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Dto.Response;

public static class PlayerDataResponseBuilder
{
    public static PlayerDataResponse Build(
        PlayerEntity player,
        PlayerResource resource,
        PlayerStage stage,
        PlayerSession session,
        List<PlayerWeapon> weapons,
        List<PlayerSkill> skills) =>
        new(
            player.JobType,
            player.Level,
            stage.MaxStage,
            session.LastWeaponId,
            session.ActiveSkills,
            resource.Gold,
            player.Exp,
            resource.EnhancementScroll,
            resource.Mithril,
            resource.Sp,
            weapons.Select(w => new WeaponInfoResponse(w.WeaponId, w.Count, w.EnhancementLevel, w.AwakeningCount)).ToList(),
            skills.Select(s => new SkillInfoResponse(s.SkillId, s.IsUnlocked)).ToList()
        );
}
