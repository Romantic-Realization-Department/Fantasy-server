using Fantasy.Common.Domain.Account.Entity.Constant;

namespace Fantasy.Common.Domain.Account.Dto.Response;

public record AccountInfoResponse(
    long Id,
    String Email,
    AccountRole Role,
    bool IsNewAccount
    );