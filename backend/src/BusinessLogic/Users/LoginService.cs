using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Security;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Data.Repositories;

namespace TaskManagementSystem.BusinessLogic.Users;

/// <summary>
/// Sequential lockout schedule: failed attempts accumulate and never reset except on a
/// successful login. Every odd attempt count from 3 onward triggers a lockout; the duration
/// grows for the first four thresholds and then holds steady at 60 minutes.
/// 3 -> 5 min, 5 -> 10 min, 7 -> 20 min, 9 -> 30 min, 11/13/15/... -> 60 min each.
/// </summary>
public class LoginService : ILoginService
{
    private static readonly (int Attempts, TimeSpan Duration)[] LockoutSchedule =
    {
        (3, TimeSpan.FromMinutes(5)),
        (5, TimeSpan.FromMinutes(10)),
        (7, TimeSpan.FromMinutes(20)),
        (9, TimeSpan.FromMinutes(30)),
    };

    private static readonly TimeSpan RepeatingLockoutDuration = TimeSpan.FromMinutes(60);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;

    public LoginService(IUserRepository userRepository, IPasswordHasher passwordHasher, TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
    }

    public async Task<User> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (user.LockedUntil is { } lockedUntil && lockedUntil > now)
        {
            throw new AccountLockedException(lockedUntil);
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await HandleFailedAttemptAsync(user, now);
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await _userRepository.UpdateAsync(user);

        return user;
    }

    private async Task HandleFailedAttemptAsync(User user, DateTime now)
    {
        user.FailedLoginAttempts++;
        var lockoutDuration = GetLockoutDuration(user.FailedLoginAttempts);
        user.LockedUntil = lockoutDuration is { } duration ? now.Add(duration) : null;

        await _userRepository.UpdateAsync(user);

        if (lockoutDuration is not null)
        {
            throw new AccountLockedException(user.LockedUntil!.Value);
        }

        throw new InvalidCredentialsException();
    }

    private static TimeSpan? GetLockoutDuration(int failedAttempts)
    {
        foreach (var (attempts, duration) in LockoutSchedule)
        {
            if (failedAttempts == attempts)
            {
                return duration;
            }
        }

        if (failedAttempts >= 11 && failedAttempts % 2 == 1)
        {
            return RepeatingLockoutDuration;
        }

        return null;
    }
}
