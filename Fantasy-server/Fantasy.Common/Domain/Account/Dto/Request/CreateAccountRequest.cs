using System.ComponentModel.DataAnnotations;

namespace Fantasy.Common.Domain.Account.Dto.Request;

public record CreateAccountRequest(
    [Required]
    [EmailAddress]
    [MaxLength(50)]
    string Email,
    
    [Required]
    [MinLength(8)]
    [MaxLength(20)]
    string Password
    );