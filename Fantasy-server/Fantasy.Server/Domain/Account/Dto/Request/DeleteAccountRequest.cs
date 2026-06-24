using System.ComponentModel.DataAnnotations;

namespace Fantasy.Server.Domain.Account.Dto.Request;

public record DeleteAccountRequest(
    [Required]
    string Password
);
