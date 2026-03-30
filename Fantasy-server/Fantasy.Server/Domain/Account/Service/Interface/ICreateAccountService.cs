using Fantasy.Server.Domain.Account.Dto.Request;

namespace Fantasy.Server.Domain.Account.Service.Interface;

public interface ICreateAccountService
{
    Task ExecuteAsync(CreateAccountRequest request);
}
