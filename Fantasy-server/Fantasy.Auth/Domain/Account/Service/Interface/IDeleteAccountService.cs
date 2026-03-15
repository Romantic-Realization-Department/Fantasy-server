using Fantasy.Common.Domain.Account.Dto.Request;

namespace Fantasy.Auth.Domain.Account.Service.Interface;

public interface IDeleteAccountService
{
    Task ExecuteAsync(DeleteAccountRequest request);
}