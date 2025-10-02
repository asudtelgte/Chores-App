using ChoreTracker.API.Data;
using ChoreTracker.API.GraphQL.Inputs;
using ChoreTracker.API.GraphQL.Mutations;
using ChoreTracker.API.GraphQL.Queries;
using ChoreTracker.API.GraphQL.Types;
using ChoreTracker.API.Models;
using ChoreTracker.API.Services;
using FluentAssertions;
using HotChocolate;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ChoreTracker.Tests;

public class ChoreIsolationTests : IDisposable
{
    private readonly ChoreTrackerDbContext _context;
    private readonly Mutation _mutation;
    private readonly Query _query;

    public ChoreIsolationTests()
    {
        var options = new DbContextOptionsBuilder<ChoreTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChoreTrackerDbContext(options);
        _mutation = new Mutation();
        _query = new Query();

        // Seed database with test users and chores
        SeedDatabase().Wait();
    }

    private async Task SeedDatabase()
    {
        var user1 = new User
        {
            Id = 1,
            Email = "user1@example.com",
            PasswordHash = "hash1",
            CreatedDate = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = 2,
            Email = "user2@example.com",
            PasswordHash = "hash2",
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var chore1 = new Chore
        {
            Id = 1,
            Name = "User 1's Chore",
            Description = "Belongs to user 1",
            Category = ChoreCategory.Daily,
            UserId = 1,
            RecurrencePattern = new RecurrencePattern { DayInterval = 1 }
        };

        var chore2 = new Chore
        {
            Id = 2,
            Name = "User 2's Chore",
            Description = "Belongs to user 2",
            Category = ChoreCategory.Weekly,
            UserId = 2,
            RecurrencePattern = new RecurrencePattern { DayInterval = 7 }
        };

        _context.Chores.AddRange(chore1, chore2);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private ICurrentUserService CreateCurrentUserService(int userId)
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        return new CurrentUserService(mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task GetChores_User1_ShouldOnlySeeTheirOwnChores()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _query.GetChores(_context, currentUserService);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("User 1's Chore");
    }

    [Fact]
    public async Task GetChores_User2_ShouldOnlySeeTheirOwnChores()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(2);

        // Act
        var result = await _query.GetChores(_context, currentUserService);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(2);
        result[0].Name.Should().Be("User 2's Chore");
    }

    [Fact]
    public async Task CreateChore_ShouldAssignToAuthenticatedUser()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new ChoreInput
        {
            Name = "New Chore",
            Description = "Test chore",
            Category = ChoreCategory.Daily,
            RecurrencePattern = new RecurrencePatternType { DayInterval = 1 }
        };

        // Act
        var result = await _mutation.CreateChore(input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.Name.Should().Be("New Chore");

        // Verify in database
        var savedChore = await _context.Chores.FindAsync(result.Id);
        savedChore.Should().NotBeNull();
        savedChore!.UserId.Should().Be(1);
    }

    [Fact]
    public async Task UpdateChore_UserCanUpdateOwnChore()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new ChoreInput
        {
            Name = "Updated Chore",
            Description = "Updated description",
            Category = ChoreCategory.Weekly,
            RecurrencePattern = new RecurrencePatternType { DayInterval = 7 }
        };

        // Act
        var result = await _mutation.UpdateChore(1, input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Chore");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateChore_UserCannotUpdateOthersChore()
    {
        // Arrange - User 1 trying to update User 2's chore
        var currentUserService = CreateCurrentUserService(1);
        var input = new ChoreInput
        {
            Name = "Hacked Chore",
            Description = "This shouldn't work",
            Category = ChoreCategory.Weekly,
            RecurrencePattern = new RecurrencePatternType { DayInterval = 7 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(
            async () => await _mutation.UpdateChore(2, input, _context, currentUserService)
        );

        // Verify chore was not modified
        var chore = await _context.Chores.FindAsync(2);
        chore!.Name.Should().Be("User 2's Chore");
    }

    [Fact]
    public async Task DeleteChore_UserCanDeleteOwnChore()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _mutation.DeleteChore(1, _context, currentUserService);

        // Assert
        result.Should().BeTrue();

        // Verify chore was soft deleted (IsActive = false)
        var deletedChore = await _context.Chores.FindAsync(1);
        deletedChore.Should().NotBeNull();
        deletedChore!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteChore_UserCannotDeleteOthersChore()
    {
        // Arrange - User 1 trying to delete User 2's chore
        var currentUserService = CreateCurrentUserService(1);

        // Act & Assert
        await Assert.ThrowsAsync<GraphQLException>(
            async () => await _mutation.DeleteChore(2, _context, currentUserService)
        );

        // Verify chore still exists
        var chore = await _context.Chores.FindAsync(2);
        chore.Should().NotBeNull();
    }

}
