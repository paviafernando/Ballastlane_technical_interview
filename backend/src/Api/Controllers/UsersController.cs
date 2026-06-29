using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagementSystem.Api.Contracts;
using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Users;
using TaskManagementSystem.Data.Repositories;

namespace TaskManagementSystem.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRegistrationService _userRegistrationService;
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRegistrationService userRegistrationService, IUserRepository userRepository)
    {
        _userRegistrationService = userRegistrationService;
        _userRepository = userRepository;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterUserRequestDto request)
    {
        try
        {
            var user = await _userRegistrationService.RegisterAsync(
                new RegisterUserRequest(request.Name, request.LastName, request.Username, request.Password, request.Birthday));

            return CreatedAtAction(nameof(GetCurrentUser), null, UserResponseDto.FromEntity(user));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ErrorResponseDto(ex.Message));
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var username = User.Identity?.Name;
        if (username is null)
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(UserResponseDto.FromEntity(user));
    }
}
