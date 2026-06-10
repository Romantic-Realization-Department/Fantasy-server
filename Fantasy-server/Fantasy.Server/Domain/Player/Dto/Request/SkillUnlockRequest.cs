using System.ComponentModel.DataAnnotations;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record SkillUnlockRequest([Required] int SkillId);
