using FluentAssertions;
using Moq;
using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Security;
using TaskManagementSystem.BusinessLogic.Users;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Data.Repositories;
using Xunit;

namespace TaskManagementSystem.BusinessLogic.UnitTests.Users;

public class UserRegistrationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly UserRegistrationService _sut;

    public UserRegistrationServiceTests()
    {
        _sut = new UserRegistrationService(_userRepositoryMock.Object, _passwordHasherMock.Object);

        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns("hashed-password");
        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);
    }

    private static RegisterUserRequest ValidRequest() =>
        new("Jane", "Doe", "janedoe", "supersecret1", new DateOnly(1990, 1, 1));

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsUserWithGeneratedId()
    {
        var result = await _sut.RegisterAsync(ValidRequest());

        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_StoresHashedPasswordNotPlainText()
    {
        _passwordHasherMock.Setup(h => h.Hash("supersecret1")).Returns("hashed-password");

        var result = await _sut.RegisterAsync(ValidRequest());

        result.PasswordHash.Should().Be("hashed-password");
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_SetsFailedLoginAttemptsToZeroByDefault()
    {
        var result = await _sut.RegisterAsync(ValidRequest());

        result.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_SetsLockedUntilToNullByDefault()
    {
        var result = await _sut.RegisterAsync(ValidRequest());

        result.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_SetsCreatedAtAndUpdatedAtTimestamps()
    {
        var before = DateTime.UtcNow;

        var result = await _sut.RegisterAsync(ValidRequest());

        var after = DateTime.UtcNow;
        result.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.UpdatedAt.Should().Be(result.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_WithEmptyOrNullUsername_ThrowsValidationException(string? username)
    {
        var request = ValidRequest() with { Username = username! };

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_WithEmptyOrNullPassword_ThrowsValidationException(string? password)
    {
        var request = ValidRequest() with { Password = password! };

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RegisterAsync_WithPasswordShorterThanMinimumLength_ThrowsValidationException()
    {
        var request = ValidRequest() with { Password = "short1" };

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RegisterAsync_WithEmptyOrNullName_ThrowsValidationException(string? name)
    {
        var request = ValidRequest() with { Name = name! };

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RegisterAsync_WithEmptyOrNullLastName_ThrowsValidationException(string? lastName)
    {
        var request = ValidRequest() with { LastName = lastName! };

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RegisterAsync_WithUsernameAlreadyTaken_ThrowsValidationException()
    {
        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("janedoe"))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Username = "janedoe" });

        var act = () => _sut.RegisterAsync(ValidRequest());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RegisterAsync_WithUsernameAlreadyTaken_DoesNotCallAddAsync()
    {
        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("janedoe"))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Username = "janedoe" });

        var act = () => _sut.RegisterAsync(ValidRequest());

        await act.Should().ThrowAsync<ValidationException>();
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }
}
