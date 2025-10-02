using ChoreTracker.API.Data;
using ChoreTracker.API.GraphQL.Inputs;
using ChoreTracker.API.GraphQL.Mutations;
using ChoreTracker.API.Models;
using ChoreTracker.API.Services;
using FluentAssertions;
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ChoreTracker.Tests;

public class AuthMutationTests : IDisposable
{
    private readonly ChoreTrackerDbContext _context;
    private readonly IAuthService _authService;
    private readonly Mutation _mutation;

    public AuthMutationTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ChoreTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChoreTrackerDbContext(options);

        // Create test configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSecretKeyThatIsAtLeast32CharactersLongForHS256!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        _authService = new AuthService(configuration);
        _mutation = new Mutation();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task Register_WithValidInput_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        var input = new RegisterInput
        {
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _mutation.Register(input, _context, _authService);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("newuser@example.com");

        // Verify user was saved to database
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        savedUser.Should().NotBeNull();
        savedUser!.PasswordHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldThrowException_Test1()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            PasswordHash = "hashedpassword",
            CreatedDate = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var input = new RegisterInput
        {
            Email = "existing@example.com",
            Password = "Password123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(
            async () => await _mutation.Register(input, _context, _authService)
        );
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldThrowException()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            PasswordHash = "hashedpassword",
            CreatedDate = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var input = new RegisterInput
        {
            Email = "existing@example.com",
            Password = "Password123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(
            async () => await _mutation.Register(input, _context, _authService)
        );
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldThrowException()
    {
        // Arrange
        var input = new RegisterInput
        {
            Email = "newuser@example.com",
            Password = "short"
        };

        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(
            async () => await _mutation.Register(input, _context, _authService)
        );
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var password = "Password123!";
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = _authService.HashPassword(password),
            CreatedDate = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var input = new LoginInput
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = await _mutation.Login(input, _context, _authService);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ShouldThrowException()
    {
        // Arrange
        var input = new LoginInput
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(
            async () => await _mutation.Login(input, _context, _authService)
        );
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ShouldThrowException()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = _authService.HashPassword("CorrectPass123!"),
            CreatedDate = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var input = new LoginInput
        {
            Email = "test@example.com",
            Password = "WrongPass456!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(
            async () => await _mutation.Login(input, _context, _authService)
        );
    }

    [Fact]
    public async Task Login_ShouldUpdateLastLoginDate()
    {
        // Arrange
        var password = "Password123!";
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = _authService.HashPassword(password),
            CreatedDate = DateTime.UtcNow,
            LastLoginDate = null
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var input = new LoginInput
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        await _mutation.Login(input, _context, _authService);

        // Assert
        var updatedUser = await _context.Users.FirstAsync(u => u.Email == "test@example.com");
        updatedUser.LastLoginDate.Should().NotBeNull();
        updatedUser.LastLoginDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
