using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManagementSystem.Api.Auth;
using TaskManagementSystem.Api.Contracts;
using TaskManagementSystem.Api.Controllers;
using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Users;
using TaskManagementSystem.Data.Entities;
using Xunit;

namespace TaskManagementSystem.Api.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<ILoginService> _loginServiceMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_loginServiceMock.Object, _jwtTokenGeneratorMock.Object);
    }

    private static User SomeUser() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jane",
        LastName = "Doe",
        Username = "janedoe",
        PasswordHash = "hashed",
    };

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        var user = SomeUser();
        var expiresAtUtc = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc);
        _loginServiceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>())).ReturnsAsync(user);
        _jwtTokenGeneratorMock.Setup(g => g.Generate(user)).Returns(new JwtToken("a.b.c", expiresAtUtc));

        var result = await _sut.Login(new LoginRequestDto("janedoe", "supersecret1"));

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = okResult.Value.Should().BeOfType<LoginResponseDto>().Subject;
        body.Token.Should().Be("a.b.c");
        body.ExpiresAtUtc.Should().Be(expiresAtUtc);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        _loginServiceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new InvalidCredentialsException());

        var result = await _sut.Login(new LoginRequestDto("janedoe", "wrong-password"));

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WhenAccountIsLocked_ReturnsLockedWithLockedUntil()
    {
        var lockedUntil = new DateTime(2026, 1, 1, 12, 5, 0, DateTimeKind.Utc);
        _loginServiceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new AccountLockedException(lockedUntil));

        var result = await _sut.Login(new LoginRequestDto("janedoe", "wrong-password"));

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status423Locked);
        var body = objectResult.Value.Should().BeOfType<AccountLockedResponseDto>().Subject;
        body.LockedUntilUtc.Should().Be(lockedUntil);
    }
}
