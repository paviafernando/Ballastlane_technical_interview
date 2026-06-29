using FluentAssertions;
using Moq;
using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Security;
using TaskManagementSystem.BusinessLogic.Users;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Data.Repositories;
using Xunit;

namespace TaskManagementSystem.BusinessLogic.UnitTests.Users;

public class LoginServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly LoginService _sut;

    public LoginServiceTests()
    {
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Now);
        _sut = new LoginService(_userRepositoryMock.Object, _passwordHasherMock.Object, _timeProviderMock.Object);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);
    }

    private static User ExistingUser(int failedLoginAttempts = 0, DateTime? lockedUntil = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jane",
        LastName = "Doe",
        Username = "janedoe",
        PasswordHash = "correctly-hashed-password",
        FailedLoginAttempts = failedLoginAttempts,
        LockedUntil = lockedUntil,
        CreatedAt = Now.UtcDateTime,
        UpdatedAt = Now.UtcDateTime,
    };

    private static LoginRequest ValidRequest(string password = "supersecret1") => new("janedoe", password);

    [Fact]
    public async Task LoginAsync_WithUnknownUsername_ThrowsInvalidCredentialsException()
    {
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("janedoe")).ReturnsAsync((User?)null);

        var act = () => _sut.LoginAsync(ValidRequest());

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ReturnsUser()
    {
        var user = ExistingUser();
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("janedoe")).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("supersecret1", user.PasswordHash)).Returns(true);

        var result = await _sut.LoginAsync(ValidRequest());

        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ResetsFailedLoginAttemptsAndClearsLock()
    {
        var user = ExistingUser(failedLoginAttempts: 4, lockedUntil: Now.UtcDateTime.AddMinutes(-1));
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("janedoe")).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("supersecret1", user.PasswordHash)).Returns(true);

        var result = await _sut.LoginAsync(ValidRequest());

        result.FailedLoginAttempts.Should().Be(0);
        result.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsExceptionBelowLockThreshold()
    {
        var user = ExistingUser(failedLoginAttempts: 0);
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("janedoe")).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);

        var act = () => _sut.LoginAsync(ValidRequest("wrong-password"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_IncrementsFailedLoginAttempts()
    {
        var user = ExistingUser(failedLoginAttempts: 1);
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("janedoe")).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);

        try
        {
            await _sut.LoginAsync(ValidRequest("wrong-password"));
        }
        catch (InvalidCredentialsException)
        {
            // expected for an attempt count below the lock threshold
        }

        user.FailedLoginAttempts.Should().Be(2);
    }

    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(4, 5, 10)]
    [InlineData(6, 7, 20)]
    [InlineData(8, 9, 30)]
    [InlineData(10, 11, 60)]
    [InlineData(12, 13, 60)]
    public async Task LoginAsync_WithWrongPasswordReachingThreshold_LocksAccountForExpectedDuration(
        int attemptsBefore, int attemptsAfter, int expectedLockMinutes)
    {
        var user = ExistingUser(failedLoginAttempts: attemptsBefore);
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("janedoe")).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);

        var act = () => _sut.LoginAsync(ValidRequest("wrong-password"));

        var exception = await act.Should().ThrowAsync<AccountLockedException>();
        exception.Which.LockedUntil.Should().Be(Now.UtcDateTime.AddMinutes(expectedLockMinutes));
        user.FailedLoginAttempts.Should().Be(attemptsAfter);
    }

    [Fact]
    public async Task LoginAsync_WhenCurrentlyLocked_ThrowsAccountLockedExceptionWithoutCheckingPassword()
    {
        var lockedUntil = Now.UtcDateTime.AddMinutes(5);
        var user = ExistingUser(failedLoginAttempts: 3, lockedUntil: lockedUntil);
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("janedoe")).ReturnsAsync(user);

        var act = () => _sut.LoginAsync(ValidRequest());

        var exception = await act.Should().ThrowAsync<AccountLockedException>();
        exception.Which.LockedUntil.Should().Be(lockedUntil);
        _passwordHasherMock.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        user.FailedLoginAttempts.Should().Be(3);
    }

    [Fact]
    public async Task LoginAsync_WhenLockHasExpired_ProceedsToCheckPassword()
    {
        var lockedUntil = Now.UtcDateTime.AddMinutes(-1);
        var user = ExistingUser(failedLoginAttempts: 3, lockedUntil: lockedUntil);
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("janedoe")).ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("supersecret1", user.PasswordHash)).Returns(true);

        var result = await _sut.LoginAsync(ValidRequest());

        result.Id.Should().Be(user.Id);
    }
}
