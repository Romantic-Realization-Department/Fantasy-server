using System.ComponentModel.DataAnnotations;

namespace Fantasy.Server.Domain.Auth.Dto.Request;

public record RefreshTokenRequest(
    [Required] string RefreshToken
);
