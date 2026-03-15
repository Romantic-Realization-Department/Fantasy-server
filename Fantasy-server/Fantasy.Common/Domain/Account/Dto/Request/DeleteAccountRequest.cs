using System.ComponentModel.DataAnnotations;

namespace Fantasy.Common.Domain.Account.Dto.Request;

public record DeleteAccountRequest(
    [Required]
    string Password
    );