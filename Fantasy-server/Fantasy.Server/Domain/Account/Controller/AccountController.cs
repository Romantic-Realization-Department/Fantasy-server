using Fantasy.Server.Domain.Account.Dto.Request;
using Fantasy.Server.Domain.Account.Service.Interface;
using Gamism.SDK.Core.Network;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy.Server.Domain.Account.Controller;

[ApiController]
[Route("v1/account")]
public class AccountController : ControllerBase
{
    private readonly ICreateAccountService _createAccountService;
    private readonly IDeleteAccountService _deleteAccountService;

    public AccountController(
        ICreateAccountService createAccountService,
        IDeleteAccountService deleteAccountService)
    {
        _createAccountService = createAccountService;
        _deleteAccountService = deleteAccountService;
    }

    [HttpPost("signup")]
    public async Task<CommonApiResponse> SignUp([FromBody] CreateAccountRequest request)
    {
        await _createAccountService.ExecuteAsync(request);
        return CommonApiResponse.Created("계정이 생성되었습니다.");
    }

    [Authorize]
    [HttpDelete]
    public async Task<CommonApiResponse> Delete([FromBody] DeleteAccountRequest request)
    {
        await _deleteAccountService.ExecuteAsync(request);
        return CommonApiResponse.Success("계정이 삭제되었습니다.");
    }
}
