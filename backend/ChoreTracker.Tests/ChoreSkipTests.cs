using ChoreTracker.API.Data;
using ChoreTracker.API.GraphQL.Inputs;
using ChoreTracker.API.GraphQL.Mutations;
using ChoreTracker.API.GraphQL.Queries;
using ChoreTracker.API.Models;
using ChoreTracker.API.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ChoreTracker.Tests;

public class ChoreSkipTests : IDisposable
{
    private readonly ChoreTrackerDbContext _context;
    private readonly Mutation _mutation;
    private readonly Query _query;

    public ChoreSkipTests()
    {
        var options = new DbContextOptionsBuilder<ChoreTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChoreTrackerDbContext(options);
        _mutation = new Mutation();
        _query = new Query();

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

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dailyChore = new Chore
        {
            Id = 1,
            Name = "Daily Chore",
            Category = ChoreCategory.Daily,
            UserId = 1,
            RecurrencePattern = new RecurrencePattern { DayInterval = 1 },
            CreatedDate = DateTime.UtcNow.AddDays(-10)
        };

        var weeklyChore = new Chore
        {
            Id = 2,
            Name = "Weekly Chore",
            Category = ChoreCategory.Weekly,
            UserId = 1,
            RecurrencePattern = new RecurrencePattern(),
            CreatedDate = DateTime.UtcNow.AddDays(-20)
        };

        _context.Chores.AddRange(dailyChore, weeklyChore);
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
    public async Task SkipChore_WithValidInput_ShouldCreateSkip()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new SkipChoreInput
        {
            ChoreId = 1,
            SkippedDeadline = DateTime.UtcNow.AddDays(-1),
            Reason = "Too busy"
        };

        // Act
        var result = await _mutation.SkipChore(input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.ChoreId.Should().Be(1);
        result.UserId.Should().Be(1);
        result.Reason.Should().Be("Too busy");
    }

    [Fact]
    public async Task SkipChore_WithoutReason_ShouldDefaultToManuallySkipped()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new SkipChoreInput
        {
            ChoreId = 1,
            SkippedDeadline = DateTime.UtcNow.AddDays(-1),
            Reason = null
        };

        // Act
        var result = await _mutation.SkipChore(input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.Reason.Should().Be("Manually skipped");
    }

    [Fact]
    public async Task SkipChore_WithNonexistentChore_ShouldThrowException()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new SkipChoreInput
        {
            ChoreId = 999,
            SkippedDeadline = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _mutation.SkipChore(input, _context, currentUserService)
        );
    }

    [Fact]
    public async Task SkipChore_WithOtherUsersChore_ShouldThrowException()
    {
        // Arrange - Create another user
        var user2 = new User
        {
            Id = 2,
            Email = "user2@example.com",
            PasswordHash = "hash2",
            CreatedDate = DateTime.UtcNow
        };
        _context.Users.Add(user2);
        await _context.SaveChangesAsync();

        var currentUserService = CreateCurrentUserService(2);
        var input = new SkipChoreInput
        {
            ChoreId = 1, // Belongs to User 1
            SkippedDeadline = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _mutation.SkipChore(input, _context, currentUserService)
        );
    }

    [Fact]
    public async Task CompleteChore_WhenLate_ShouldAutoRecordSkips()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Complete a chore from the past
        var oldCompletion = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow.AddDays(-5),
            NecessityRating = 3
        };
        _context.ChoreCompletions.Add(oldCompletion);
        await _context.SaveChangesAsync();

        var input = new CompleteChoreInput
        {
            ChoreId = 1,
            NecessityRating = 4,
            Notes = "Late completion"
        };

        // Act - Complete today (4 days late for a daily chore)
        var result = await _mutation.CompleteChore(input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();

        // Check that skips were automatically recorded
        var skips = await _context.ChoreSkips
            .Where(s => s.ChoreId == 1 && s.UserId == 1)
            .ToListAsync();

        skips.Should().NotBeEmpty();
        skips.All(s => s.Reason == "Automatically recorded - deadline passed").Should().BeTrue();
    }

    [Fact]
    public async Task GetChoreSkipAnalytics_ShouldReturnAnalyticsForChoresWithActivity()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Add some completions and skips
        var completion1 = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow.AddDays(-8),
            NecessityRating = 4
        };

        var completion2 = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow.AddDays(-4),
            NecessityRating = 2 // Lower rating after skipping
        };

        _context.ChoreCompletions.AddRange(completion1, completion2);

        var skip = new ChoreSkip
        {
            ChoreId = 1,
            UserId = 1,
            SkippedDeadline = DateTime.UtcNow.AddDays(-6),
            Reason = "Test skip"
        };

        _context.ChoreSkips.Add(skip);
        await _context.SaveChangesAsync();

        // Act
        var result = await _query.GetChoreSkipAnalytics(_context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        var analytics = result.FirstOrDefault(a => a.ChoreId == 1);
        analytics.Should().NotBeNull();
        analytics!.ChoreName.Should().Be("Daily Chore");
        analytics.TotalSkips.Should().Be(1);
        analytics.TotalCompletions.Should().Be(2);
        analytics.SkipRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetChoreSkipAnalytics_WithHighSkipRate_ShouldRecommendReducingFrequency()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Add more skips than completions
        for (int i = 0; i < 5; i++)
        {
            var skip = new ChoreSkip
            {
                ChoreId = 1,
                UserId = 1,
                SkippedDeadline = DateTime.UtcNow.AddDays(-10 + i),
                Reason = $"Skip {i}"
            };
            _context.ChoreSkips.Add(skip);
        }

        var completion = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow,
            NecessityRating = 2
        };
        _context.ChoreCompletions.Add(completion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _query.GetChoreSkipAnalytics(_context, currentUserService);

        // Assert
        var analytics = result.FirstOrDefault(a => a.ChoreId == 1);
        analytics.Should().NotBeNull();
        analytics!.FrequencyRecommendation.Should().Contain("reducing frequency");
    }

    [Fact]
    public async Task GetChoreSkipAnalytics_ShouldCalculateNecessityDifference()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // On-time completion (high necessity)
        var onTimeCompletion = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow.AddDays(-10),
            NecessityRating = 5
        };

        // Skip
        var skip = new ChoreSkip
        {
            ChoreId = 1,
            UserId = 1,
            SkippedDeadline = DateTime.UtcNow.AddDays(-8),
            Reason = "Skipped"
        };

        // After-skip completion (low necessity - suggests frequency too high)
        var afterSkipCompletion = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow.AddDays(-6),
            NecessityRating = 2
        };

        _context.ChoreCompletions.AddRange(onTimeCompletion, afterSkipCompletion);
        _context.ChoreSkips.Add(skip);
        await _context.SaveChangesAsync();

        // Act
        var result = await _query.GetChoreSkipAnalytics(_context, currentUserService);

        // Assert
        var analytics = result.FirstOrDefault(a => a.ChoreId == 1);
        analytics.Should().NotBeNull();
        analytics!.AverageNecessityAfterSkip.Should().BeLessThan(analytics.AverageNecessityOnTime);
        analytics.NecessityDifference.Should().BeLessThan(0);
    }

    [Fact]
    public async Task AutoRecordSkips_ShouldNotDuplicateExistingSkips()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Manually record a skip
        var manualSkip = new ChoreSkip
        {
            ChoreId = 1,
            UserId = 1,
            SkippedDeadline = DateTime.UtcNow.AddDays(-3).Date,
            Reason = "Manual skip"
        };
        _context.ChoreSkips.Add(manualSkip);

        // Old completion
        var oldCompletion = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow.AddDays(-5),
            NecessityRating = 3
        };
        _context.ChoreCompletions.Add(oldCompletion);
        await _context.SaveChangesAsync();

        var input = new CompleteChoreInput
        {
            ChoreId = 1,
            NecessityRating = 4
        };

        // Act - Complete today (should auto-record skips but not duplicate the manual one)
        await _mutation.CompleteChore(input, _context, currentUserService);

        // Assert
        var skips = await _context.ChoreSkips
            .Where(s => s.ChoreId == 1 && s.SkippedDeadline.Date == DateTime.UtcNow.AddDays(-3).Date)
            .ToListAsync();

        skips.Should().HaveCount(1, "Should not duplicate existing skip");
        skips[0].Reason.Should().Be("Manual skip");
    }
}
