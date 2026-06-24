using Fantasy.Server.Domain.Account.Entity.Constant;

namespace Fantasy.Server.Domain.Account.Dto.Response;

public record AccountInfoResponse(
    long Id,
    string Email,
    AccountRole Role,
    bool IsNewAccount
);
