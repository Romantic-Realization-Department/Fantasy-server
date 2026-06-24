using System.ComponentModel.DataAnnotations;

namespace Fantasy.Server.Domain.Auth.Dto.Request;

public record LoginRequest(
    [Required]
    [EmailAddress]
    [MaxLength(50)]
    string Email,

    [Required]
    [MinLength(8)]
    [MaxLength(20)]
    string Password
);
