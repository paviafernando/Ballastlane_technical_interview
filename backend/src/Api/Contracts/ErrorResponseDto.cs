namespace TaskManagementSystem.Api.Contracts;

public record ErrorResponseDto(string Message);

public record AccountLockedResponseDto(string Message, DateTime LockedUntilUtc);
