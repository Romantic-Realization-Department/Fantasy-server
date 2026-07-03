using Fantasy.Server.Domain.Account.Dto.Request;
using Fantasy.Server.Domain.Account.Service.Interface;
using Gamism.SDK.Core.Network;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using Gamism.SDK.Extensions.AspNetCore.Swagger;
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

    [ApiDoc("회원가입", "이메일과 비밀번호로 새 계정을 생성합니다.")]
    [ApiError(typeof(ConflictException), "이미 사용중인 이메일입니다.")]
    [HttpPost("signup")]
    public async Task<CommonApiResponse> SignUp([FromBody] CreateAccountRequest request)
    {
        await _createAccountService.ExecuteAsync(request);
        return CommonApiResponse.Created("계정이 생성되었습니다.");
    }

    [ApiDoc("계정 삭제", "비밀번호 확인 후 현재 계정을 삭제합니다.")]
    [ApiError(typeof(UnauthorizedException), "이메일 또는 비밀번호가 올바르지 않습니다.")]
    [Authorize]
    [HttpDelete]
    public async Task<CommonApiResponse> Delete([FromBody] DeleteAccountRequest request)
    {
        await _deleteAccountService.ExecuteAsync(request);
        return CommonApiResponse.Success("계정이 삭제되었습니다.");
    }
}
