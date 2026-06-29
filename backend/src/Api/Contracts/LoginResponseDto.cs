namespace TaskManagementSystem.Api.Contracts;

public record LoginResponseDto(string Token, DateTime ExpiresAtUtc);
