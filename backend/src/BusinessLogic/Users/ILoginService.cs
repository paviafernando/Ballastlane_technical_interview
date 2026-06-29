using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.BusinessLogic.Users;

public interface ILoginService
{
    Task<User> LoginAsync(LoginRequest request);
}
