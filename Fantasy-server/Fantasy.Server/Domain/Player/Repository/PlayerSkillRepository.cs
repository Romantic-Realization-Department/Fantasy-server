using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Player.Repository;

public class PlayerSkillRepository : IPlayerSkillRepository
{
    private readonly AppDbContext _db;

    public PlayerSkillRepository(AppDbContext db) => _db = db;

    public async Task<List<PlayerSkill>> FindAllByPlayerIdAsync(long playerId)
        => await _db.PlayerSkills
            .AsNoTracking()
            .Where(skill => skill.PlayerId == playerId)
            .ToListAsync();

    public async Task UpsertRangeAsync(long playerId, List<SkillChangeItem> items)
    {
        List<SkillChangeItem> normalizedItems = items
            .GroupBy(item => item.SkillId)
            .Select(group => group.Last())
            .ToList();

        List<int> skillIds = normalizedItems.Select(item => item.SkillId).ToList();
        Dictionary<int, PlayerSkill> existing = await _db.PlayerSkills
            .Where(skill => skill.PlayerId == playerId && skillIds.Contains(skill.SkillId))
            .ToDictionaryAsync(skill => skill.SkillId);

        foreach (SkillChangeItem item in normalizedItems)
        {
            if (existing.TryGetValue(item.SkillId, out PlayerSkill? skill))
            {
                skill.Update(item.IsUnlocked);
                _db.PlayerSkills.Update(skill);
                continue;
            }

            await _db.PlayerSkills.AddAsync(
                PlayerSkill.Create(playerId, item.SkillId, item.IsUnlocked));
        }

        await _db.SaveChangesAsync();
    }
}
