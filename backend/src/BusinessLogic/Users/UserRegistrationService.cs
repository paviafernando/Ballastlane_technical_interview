using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Security;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Data.Repositories;

namespace TaskManagementSystem.BusinessLogic.Users;

public class UserRegistrationService : IUserRegistrationService
{
    private const int MinimumPasswordLength = 8;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserRegistrationService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> RegisterAsync(RegisterUserRequest request)
    {
        ValidateRequest(request);

        var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUser is not null)
        {
            throw new ValidationException($"Username '{request.Username}' is already taken.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            LastName = request.LastName,
            Username = request.Username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Birthday = request.Birthday,
            FailedLoginAttempts = 0,
            LockedUntil = null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        return await _userRepository.AddAsync(user);
    }

    private static void ValidateRequest(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ValidationException("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Password is required.");
        }

        if (request.Password.Length < MinimumPasswordLength)
        {
            throw new ValidationException($"Password must be at least {MinimumPasswordLength} characters long.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ValidationException("Last name is required.");
        }
    }
}
