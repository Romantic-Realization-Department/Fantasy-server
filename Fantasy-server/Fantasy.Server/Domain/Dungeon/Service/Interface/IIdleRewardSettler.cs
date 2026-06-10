using Fantasy.Server.Domain.LevelUp.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Dungeon.Service.Interface;

public record IdleRewardResult(long EarnedGold, long EarnedXp, long NewMaxStage, List<LevelUpResult> LevelUps);

public interface IIdleRewardSettler
{
    /// <summary>
    /// 경과 시간 기반 방치 보상을 계산하고 엔티티에 반영한다 (DB 저장 없음 — 호출자가 트랜잭션에서 저장).
    /// elapsedSeconds <= 0이면 변경 없이 Empty 결과를 반환한다.
    /// </summary>
    Task<IdleRewardResult> SettleAsync(
        PlayerEntity player,
        PlayerResource resource,
        PlayerStage stage,
        PlayerSession session,
        List<PlayerWeapon> weapons,
        List<PlayerSkill> skills);
}
