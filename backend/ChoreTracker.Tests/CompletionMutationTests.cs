using ChoreTracker.API.Data;
using ChoreTracker.API.GraphQL.Inputs;
using ChoreTracker.API.GraphQL.Mutations;
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

public class CompletionMutationTests : IDisposable
{
    private readonly ChoreTrackerDbContext _context;
    private readonly Mutation _mutation;

    public CompletionMutationTests()
    {
        var options = new DbContextOptionsBuilder<ChoreTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChoreTrackerDbContext(options);
        _mutation = new Mutation();

        SeedDatabase().Wait();
    }

    private async Task SeedDatabase()
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedDate = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = 2,
            Email = "user2@example.com",
            PasswordHash = "hash2",
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.AddRange(user, user2);
        await _context.SaveChangesAsync();

        var chore = new Chore
        {
            Id = 1,
            Name = "Test Chore",
            Category = ChoreCategory.Daily,
            UserId = 1,
            RecurrencePattern = new RecurrencePattern { DayInterval = 1 }
        };

        var otherChore = new Chore
        {
            Id = 2,
            Name = "Other User's Chore",
            Category = ChoreCategory.Daily,
            UserId = 2,
            RecurrencePattern = new RecurrencePattern { DayInterval = 1 }
        };

        _context.Chores.AddRange(chore, otherChore);
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
    public async Task CompleteChore_WithValidInput_ShouldCreateCompletion()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new CompleteChoreInput
        {
            ChoreId = 1,
            NecessityRating = 4,
            Notes = "Completed successfully"
        };

        // Act
        var result = await _mutation.CompleteChore(input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.ChoreId.Should().Be(1);
        result.NecessityRating.Should().Be(4);
        result.Notes.Should().Be("Completed successfully");
        result.UserId.Should().Be(1);
        result.CompletionDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CompleteChore_WithValidRatingRange_ShouldAccept()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act & Assert - Test all valid ratings
        for (int rating = 1; rating <= 5; rating++)
        {
            var input = new CompleteChoreInput
            {
                ChoreId = 1,
                NecessityRating = rating,
                Notes = $"Rating {rating}"
            };

            var result = await _mutation.CompleteChore(input, _context, currentUserService);
            result.Should().NotBeNull();
            result.NecessityRating.Should().Be(rating);
        }
    }

    [Fact]
    public async Task CompleteChore_WithRatingTooLow_ShouldThrowException()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new CompleteChoreInput
        {
            ChoreId = 1,
            NecessityRating = 0,
            Notes = "Invalid rating"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _mutation.CompleteChore(input, _context, currentUserService)
        );
    }

    [Fact]
    public async Task CompleteChore_WithRatingTooHigh_ShouldThrowException()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new CompleteChoreInput
        {
            ChoreId = 1,
            NecessityRating = 6,
            Notes = "Invalid rating"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _mutation.CompleteChore(input, _context, currentUserService)
        );
    }

    [Fact]
    public async Task CompleteChore_WithNonexistentChore_ShouldThrowException()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new CompleteChoreInput
        {
            ChoreId = 999,
            NecessityRating = 3,
            Notes = "Does not exist"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _mutation.CompleteChore(input, _context, currentUserService)
        );
    }

    [Fact]
    public async Task CompleteChore_WithOtherUsersChore_ShouldThrowException()
    {
        // Arrange - User 1 trying to complete User 2's chore
        var currentUserService = CreateCurrentUserService(1);
        var input = new CompleteChoreInput
        {
            ChoreId = 2, // Belongs to User 2
            NecessityRating = 3,
            Notes = "Unauthorized"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _mutation.CompleteChore(input, _context, currentUserService)
        );
    }

    [Fact]
    public async Task CompleteChore_WithoutNotes_ShouldSucceed()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new CompleteChoreInput
        {
            ChoreId = 1,
            NecessityRating = 3,
            Notes = null
        };

        // Act
        var result = await _mutation.CompleteChore(input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.Notes.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCompletionRating_WithValidInput_ShouldUpdate()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Create a completion first
        var completion = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow,
            NecessityRating = 3,
            Notes = "Original"
        };
        _context.ChoreCompletions.Add(completion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _mutation.UpdateCompletionRating(
            completion.Id,
            5,
            "Updated notes",
            _context,
            currentUserService
        );

        // Assert
        result.Should().NotBeNull();
        result!.NecessityRating.Should().Be(5);
        result.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdateCompletionRating_WithValidRatingRange_ShouldAccept()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Create a completion first
        var completion = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow,
            NecessityRating = 3
        };
        _context.ChoreCompletions.Add(completion);
        await _context.SaveChangesAsync();

        // Act & Assert - Test all valid ratings
        for (int rating = 1; rating <= 5; rating++)
        {
            var result = await _mutation.UpdateCompletionRating(
                completion.Id,
                rating,
                null,
                _context,
                currentUserService
            );

            result.Should().NotBeNull();
            result!.NecessityRating.Should().Be(rating);
        }
    }

    [Fact]
    public async Task UpdateCompletionRating_WithRatingTooLow_ShouldThrowException()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        var completion = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow,
            NecessityRating = 3
        };
        _context.ChoreCompletions.Add(completion);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _mutation.UpdateCompletionRating(
                completion.Id,
                0,
                null,
                _context,
                currentUserService
            )
        );
    }

    [Fact]
    public async Task UpdateCompletionRating_WithRatingTooHigh_ShouldThrowException()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        var completion = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow,
            NecessityRating = 3
        };
        _context.ChoreCompletions.Add(completion);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _mutation.UpdateCompletionRating(
                completion.Id,
                6,
                null,
                _context,
                currentUserService
            )
        );
    }

    [Fact]
    public async Task UpdateCompletionRating_WithNonexistentCompletion_ShouldReturnNull()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _mutation.UpdateCompletionRating(
            999,
            3,
            null,
            _context,
            currentUserService
        );

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCompletionRating_WithOtherUsersCompletion_ShouldReturnNull()
    {
        // Arrange - Create completion for User 2
        var completion = new ChoreCompletion
        {
            ChoreId = 2,
            UserId = 2,
            CompletionDate = DateTime.UtcNow,
            NecessityRating = 3
        };
        _context.ChoreCompletions.Add(completion);
        await _context.SaveChangesAsync();

        // User 1 tries to update User 2's completion
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _mutation.UpdateCompletionRating(
            completion.Id,
            5,
            null,
            _context,
            currentUserService
        );

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserPreferences_WithValidInput_ShouldUpdate()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _mutation.UpdateUserPreferences(6, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.DayStartHour.Should().Be(6);
        result.UserId.Should().Be(1);
        result.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateUserPreferences_WithExistingPreferences_ShouldUpdate()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Create existing preferences
        var prefs = new UserPreferences
        {
            UserId = 1,
            DayStartHour = 3,
            CreatedDate = DateTime.UtcNow.AddDays(-10),
            ModifiedDate = DateTime.UtcNow.AddDays(-10)
        };
        _context.UserPreferences.Add(prefs);
        await _context.SaveChangesAsync();

        var oldCreatedDate = prefs.CreatedDate;

        // Act
        var result = await _mutation.UpdateUserPreferences(8, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.DayStartHour.Should().Be(8);
        result.CreatedDate.Should().Be(oldCreatedDate, "CreatedDate should not change");
        result.ModifiedDate.Should().BeAfter(oldCreatedDate, "ModifiedDate should be updated");
    }

    [Fact]
    public async Task UpdateUserPreferences_WithValidHourRange_ShouldAccept()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act & Assert - Test boundary values
        var result0 = await _mutation.UpdateUserPreferences(0, _context, currentUserService);
        result0.DayStartHour.Should().Be(0);

        var result23 = await _mutation.UpdateUserPreferences(23, _context, currentUserService);
        result23.DayStartHour.Should().Be(23);
    }

    [Fact]
    public async Task UpdateUserPreferences_WithNegativeHour_ShouldThrowException()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _mutation.UpdateUserPreferences(-1, _context, currentUserService)
        );
    }

    [Fact]
    public async Task UpdateUserPreferences_WithHourOver23_ShouldThrowException()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _mutation.UpdateUserPreferences(24, _context, currentUserService)
        );
    }
}
