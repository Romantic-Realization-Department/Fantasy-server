using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.LevelUp.Dto.Response;
using Fantasy.Server.Domain.LevelUp.Service.Interface;
using Fantasy.Server.Domain.Player.Entity;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.LevelUp.Service;

public class LevelUpService : ILevelUpService
{
    private const long MaxLevel = 100;

    private readonly IGameDataCacheService _gameDataCacheService;

    public LevelUpService(IGameDataCacheService gameDataCacheService)
    {
        _gameDataCacheService = gameDataCacheService;
    }

    public async Task<List<LevelUpResult>> ApplyExpAsync(PlayerEntity player, PlayerResource resource, long earnedExp)
    {
        var levelTable = await _gameDataCacheService.GetLevelTableAsync();
        var levelUps = new List<LevelUpResult>();

        while (player.Level < MaxLevel && levelTable.TryGetValue(player.Level, out var current))
        {
            var remaining = current.RequiredExp - player.Exp;
            if (earnedExp < remaining)
                break;

            earnedExp -= remaining;
            player.UpdateLevel(player.Level + 1);
            player.UpdateExp(0);
            resource.UpdateChangeData(null, null, resource.Sp + current.RewardSp);
            levelUps.Add(new LevelUpResult(player.Level, current.RewardSp));
        }

        player.UpdateExp(player.Exp + earnedExp);
        return levelUps;
    }
}
