using Fantasy.Common.Domain.Account.Dto.Request;

namespace Fantasy.Auth.Domain.Account.Service.Interface;

public interface ICreateAccountService
{
    Task ExecuteAsync(CreateAccountRequest request);
}