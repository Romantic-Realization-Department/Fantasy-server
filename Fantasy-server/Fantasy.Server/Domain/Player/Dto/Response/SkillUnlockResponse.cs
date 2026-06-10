using Fantasy.Server.Domain.Dungeon.Dto.Response;

namespace Fantasy.Server.Domain.Player.Dto.Response;

public record SkillUnlockResponse(bool WasAlreadyUnlocked, ChangesDto Changes, PlayerDataResponse Player);
