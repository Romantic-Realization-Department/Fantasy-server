using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Entity;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IPlayerSkillRepository
{
    Task<List<PlayerSkill>> FindAllByPlayerIdAsync(long playerId);
    Task UpsertRangeAsync(long playerId, List<SkillChangeItem> items);
}
