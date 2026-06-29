using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagementSystem.Api.Auth;
using TaskManagementSystem.Api.Contracts;
using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Users;

namespace TaskManagementSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly ILoginService _loginService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthController(ILoginService loginService, IJwtTokenGenerator jwtTokenGenerator)
    {
        _loginService = loginService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        try
        {
            var user = await _loginService.LoginAsync(new LoginRequest(request.Username, request.Password));
            var token = _jwtTokenGenerator.Generate(user);

            return Ok(new LoginResponseDto(token.Value, token.ExpiresAtUtc));
        }
        catch (AccountLockedException ex)
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                new AccountLockedResponseDto(ex.Message, ex.LockedUntil));
        }
        catch (InvalidCredentialsException ex)
        {
            return Unauthorized(new ErrorResponseDto(ex.Message));
        }
    }
}
