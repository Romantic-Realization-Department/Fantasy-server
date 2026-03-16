using Fantasy.Auth.Domain.Account.Service.Interface;
using Fantasy.Common.Domain.Account.Dto.Request;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy.Auth.Domain.Account.Controller;

[ApiController]
[Route("v1/account")]
public class AccountController : ControllerBase
{
    private readonly ICreateAccountService _createAccountService;
    private readonly IDeleteAccountService _deleteAccountService;

    public AccountController(
        ICreateAccountService createAccountService,
        IDeleteAccountService deleteAccountService
    )
    {
        _createAccountService = createAccountService;
        _deleteAccountService = deleteAccountService;   
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp(
        [FromBody] CreateAccountRequest request
        )
    {
        await _createAccountService.ExecuteAsync(request);
        return Created();
    }
    
    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromBody] DeleteAccountRequest request
        )
    {
        await _deleteAccountService.ExecuteAsync(request);
        return NoContent();
    }
}