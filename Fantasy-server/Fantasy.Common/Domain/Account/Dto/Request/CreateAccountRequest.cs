namespace Fantasy.Common.Domain.Account.Dto.Request;

public record CreateAccountRequest(
    string Email,
    string Password
    );