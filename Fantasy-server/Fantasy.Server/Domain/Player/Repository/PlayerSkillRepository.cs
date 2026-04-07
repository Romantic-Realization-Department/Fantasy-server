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
            .Where(s => s.PlayerId == playerId)
            .ToListAsync();

    public async Task UpsertRangeAsync(long playerId, List<SkillChangeItem> items)
    {
        var skillIds = items.Select(i => i.SkillId).ToList();
        var existing = await _db.PlayerSkills
            .Where(s => s.PlayerId == playerId && skillIds.Contains(s.SkillId))
            .ToListAsync();

        foreach (var item in items)
        {
            var skill = existing.FirstOrDefault(s => s.SkillId == item.SkillId);
            if (skill != null)
            {
                skill.Update(item.IsUnlocked);
                _db.PlayerSkills.Update(skill);
            }
            else
            {
                await _db.PlayerSkills.AddAsync(
                    PlayerSkill.Create(playerId, item.SkillId, item.IsUnlocked));
            }
        }

        await _db.SaveChangesAsync();
    }
}
