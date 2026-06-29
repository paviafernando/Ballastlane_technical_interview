using FluentAssertions;
using TaskManagementSystem.Database.Security;
using Xunit;

namespace TaskManagementSystem.Database.UnitTests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        var result = _sut.Hash("supersecret1");

        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_DoesNotReturnThePlainTextPassword()
    {
        var result = _sut.Hash("supersecret1");

        result.Should().NotBe("supersecret1");
    }

    [Fact]
    public void Hash_CalledTwiceWithSameInput_ProducesDifferentHashes()
    {
        var firstHash = _sut.Hash("supersecret1");
        var secondHash = _sut.Hash("supersecret1");

        firstHash.Should().NotBe(secondHash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _sut.Hash("supersecret1");

        var result = _sut.Verify("supersecret1", hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("supersecret1");

        var result = _sut.Verify("wrong-password", hash);

        result.Should().BeFalse();
    }
}
