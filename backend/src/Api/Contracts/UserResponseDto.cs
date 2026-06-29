using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Api.Contracts;

public record UserResponseDto(Guid Id, string Name, string LastName, string Username, DateOnly? Birthday)
{
    public static UserResponseDto FromEntity(User user) =>
        new(user.Id, user.Name, user.LastName, user.Username, user.Birthday);
}
