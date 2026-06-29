using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.BusinessLogic.Users;

public interface IUserRegistrationService
{
    Task<User> RegisterAsync(RegisterUserRequest request);
}
