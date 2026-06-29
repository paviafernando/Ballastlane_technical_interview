using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User> AddAsync(User user);
    Task<User> UpdateAsync(User user);
}
