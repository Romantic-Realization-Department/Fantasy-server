using Fantasy.Server.Domain.Account.Dto.Request;

namespace Fantasy.Server.Domain.Account.Service.Interface;

public interface IDeleteAccountService
{
    Task ExecuteAsync(DeleteAccountRequest request);
}
