namespace TaskManagementSystem.Api.Contracts;

public record RegisterUserRequestDto(
    string Name,
    string LastName,
    string Username,
    string Password,
    DateOnly? Birthday);
