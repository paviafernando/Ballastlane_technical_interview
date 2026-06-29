using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Data.Entities;
using TaskManagementSystem.Data.Repositories;

namespace TaskManagementSystem.Database.Repositories;

public class UserRepository : IUserRepository
{
    private readonly TaskManagementDbContext _dbContext;

    public UserRepository(TaskManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _dbContext.Users.SingleOrDefaultAsync(u => u.Username == username);

    public async Task<User> AddAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }
}
