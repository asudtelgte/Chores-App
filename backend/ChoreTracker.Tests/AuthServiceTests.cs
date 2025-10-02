using ChoreTracker.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ChoreTracker.Tests;

public class AuthServiceTests
{
    private readonly IAuthService _authService;

    public AuthServiceTests()
    {
        // Create test configuration with JWT settings
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Jwt:Key"] = "TestSecretKeyThatIsAtLeast32CharactersLongForHS256!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        _authService = new AuthService(configuration);
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var hashedPassword = _authService.HashPassword(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
        hashedPassword.Should().StartWith("$2a$"); // BCrypt hash format
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123";
        var hashedPassword = _authService.HashPassword(password);

        // Act
        var result = _authService.VerifyPassword(password, hashedPassword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123";
        var wrongPassword = "WrongPassword456";
        var hashedPassword = _authService.HashPassword(password);

        // Act
        var result = _authService.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GenerateJwtToken_ShouldReturnValidToken()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";

        // Act
        var token = _authService.GenerateJwtToken(userId, email);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts: header.payload.signature
    }

    [Fact]
    public void GenerateJwtToken_WithDifferentUsers_ShouldGenerateDifferentTokens()
    {
        // Arrange & Act
        var token1 = _authService.GenerateJwtToken(1, "user1@example.com");
        var token2 = _authService.GenerateJwtToken(2, "user2@example.com");

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void HashPassword_WithSamePassword_ShouldGenerateDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var hash1 = _authService.HashPassword(password);
        var hash2 = _authService.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt uses random salt

        // But both should verify successfully
        _authService.VerifyPassword(password, hash1).Should().BeTrue();
        _authService.VerifyPassword(password, hash2).Should().BeTrue();
    }
}
