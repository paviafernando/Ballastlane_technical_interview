using TaskManagementSystem.Data.Entities;

namespace TaskManagementSystem.Api.Auth;

public interface IJwtTokenGenerator
{
    JwtToken Generate(User user);
}

public record JwtToken(string Value, DateTime ExpiresAtUtc);
