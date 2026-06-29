using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManagementSystem.Api.Contracts;
using TaskManagementSystem.Api.Controllers;
using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Users;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Data.Repositories;
using Xunit;

namespace TaskManagementSystem.Api.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserRegistrationService> _userRegistrationServiceMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _sut = new UsersController(_userRegistrationServiceMock.Object, _userRepositoryMock.Object);
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
    public async Task Register_WithValidData_ReturnsCreatedWithUser()
    {
        var user = SomeUser();
        _userRegistrationServiceMock
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterUserRequest>()))
            .ReturnsAsync(user);

        var result = await _sut.Register(new RegisterUserRequestDto("Jane", "Doe", "janedoe", "supersecret1", null));

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var body = createdResult.Value.Should().BeOfType<UserResponseDto>().Subject;
        body.Username.Should().Be("janedoe");
    }

    [Fact]
    public async Task Register_WithInvalidData_ReturnsBadRequest()
    {
        _userRegistrationServiceMock
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterUserRequest>()))
            .ThrowsAsync(new ValidationException("Username is required."));

        var result = await _sut.Register(new RegisterUserRequestDto("Jane", "Doe", "", "supersecret1", null));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserExists_ReturnsOkWithUser()
    {
        var user = SomeUser();
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("janedoe")).ReturnsAsync(user);
        _sut.ControllerContext = ApiUnitTestHelpers.CreateAuthenticatedControllerContext("janedoe");

        var result = await _sut.GetCurrentUser();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = okResult.Value.Should().BeOfType<UserResponseDto>().Subject;
        body.Username.Should().Be("janedoe");
    }
}
