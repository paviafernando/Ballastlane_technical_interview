namespace TaskManagementSystem.BusinessLogic.Users;

public record RegisterUserRequest(
    string Name,
    string LastName,
    string Username,
    string Password,
    DateOnly? Birthday);
