using ChoreTracker.API.Data;
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

public class QueryTests : IDisposable
{
    private readonly ChoreTrackerDbContext _context;
    private readonly Query _query;

    public QueryTests()
    {
        var options = new DbContextOptionsBuilder<ChoreTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChoreTrackerDbContext(options);
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

        // Add user preferences
        var prefs = new UserPreferences
        {
            UserId = 1,
            DayStartHour = 3,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        _context.UserPreferences.Add(prefs);

        // Add daily chore
        var dailyChore = new Chore
        {
            Id = 1,
            Name = "Daily Chore",
            Category = ChoreCategory.Daily,
            UserId = 1,
            RecurrencePattern = new RecurrencePattern { DayInterval = 1 },
            CreatedDate = DateTime.UtcNow.AddDays(-5)
        };
        _context.Chores.Add(dailyChore);

        // Add weekly chore
        var weeklyChore = new Chore
        {
            Id = 2,
            Name = "Weekly Chore",
            Category = ChoreCategory.Weekly,
            UserId = 1,
            RecurrencePattern = new RecurrencePattern(),
            CreatedDate = DateTime.UtcNow.AddDays(-10)
        };
        _context.Chores.Add(weeklyChore);

        // Add monthly chore
        var monthlyChore = new Chore
        {
            Id = 3,
            Name = "Monthly Chore",
            Category = ChoreCategory.Monthly,
            UserId = 1,
            RecurrencePattern = new RecurrencePattern { DayOfMonth = 15 },
            CreatedDate = DateTime.UtcNow.AddMonths(-1)
        };
        _context.Chores.Add(monthlyChore);

        // Add special chore
        var specialChore = new Chore
        {
            Id = 4,
            Name = "Special Event",
            Category = ChoreCategory.Special,
            UserId = 1,
            Deadline = DateTime.UtcNow.AddDays(7)
        };
        _context.Chores.Add(specialChore);

        await _context.SaveChangesAsync();

        // Add completion for daily chore (today)
        var completion1 = new ChoreCompletion
        {
            ChoreId = 1,
            UserId = 1,
            CompletionDate = DateTime.UtcNow,
            NecessityRating = 4,
            Notes = "Completed today"
        };
        _context.ChoreCompletions.Add(completion1);

        // Add completion for weekly chore (this week)
        var completion2 = new ChoreCompletion
        {
            ChoreId = 2,
            UserId = 1,
            CompletionDate = DateTime.UtcNow.AddDays(-2),
            NecessityRating = 3,
            Notes = "Completed this week"
        };
        _context.ChoreCompletions.Add(completion2);

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
    public async Task GetChores_ShouldReturnAllChoresForUser()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _query.GetChores(_context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result.Should().Contain(c => c.Name == "Daily Chore");
        result.Should().Contain(c => c.Name == "Weekly Chore");
        result.Should().Contain(c => c.Name == "Monthly Chore");
        result.Should().Contain(c => c.Name == "Special Event");
    }

    [Fact]
    public async Task GetChores_ShouldMarkCompletedChoresCorrectly()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _query.GetChores(_context, currentUserService);

        // Assert
        var dailyChore = result.First(c => c.Name == "Daily Chore");
        dailyChore.IsCompletedForPeriod.Should().BeTrue("Daily chore was completed today");

        var weeklyChore = result.First(c => c.Name == "Weekly Chore");
        weeklyChore.IsCompletedForPeriod.Should().BeTrue("Weekly chore was completed this week");

        var monthlyChore = result.First(c => c.Name == "Monthly Chore");
        monthlyChore.IsCompletedForPeriod.Should().BeFalse("Monthly chore has no completions");

        var specialChore = result.First(c => c.Name == "Special Event");
        specialChore.IsCompletedForPeriod.Should().BeFalse("Special chore has no completions");
    }

    [Fact]
    public async Task GetChore_WithValidId_ShouldReturnChore()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _query.GetChore(1, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Daily Chore");
        result.Category.Should().Be(ChoreCategory.Daily);
    }

    [Fact]
    public async Task GetChore_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _query.GetChore(999, _context, currentUserService);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChore_WithOtherUsersChore_ShouldReturnNull()
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

        // Act - Try to get user 1's chore
        var result = await _query.GetChore(1, _context, currentUserService);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUpcomingChores_ShouldReturnChoresDueInPeriod()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _query.GetUpcomingChores(30, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().Contain(c => c.Name == "Special Event", "Special chore is due in 7 days");
    }

    [Fact]
    public async Task GetUpcomingChores_ShouldOnlyReturnActiveChores()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Mark a chore as inactive
        var chore = await _context.Chores.FindAsync(1);
        chore!.IsActive = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _query.GetUpcomingChores(30, _context, currentUserService);

        // Assert
        result.Should().NotContain(c => c.Name == "Daily Chore", "Inactive chores should not appear");
    }

    [Fact]
    public async Task GetCompletionHistory_ShouldReturnAllCompletions()
    {
        // Arrange & Act
        var result = _query.GetCompletionHistory(_context);

        // Assert
        var completions = await result.ToListAsync();
        completions.Should().HaveCount(2);
        completions.Should().Contain(c => c.Notes == "Completed today");
        completions.Should().Contain(c => c.Notes == "Completed this week");
    }

    [Fact]
    public async Task GetUserPreferences_ShouldReturnPreferencesForUser()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);

        // Act
        var result = await _query.GetUserPreferences(_context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result!.DayStartHour.Should().Be(3);
        result.UserId.Should().Be(1);
    }

    [Fact]
    public async Task GetUserPreferences_WithNoPreferences_ShouldReturnNull()
    {
        // Arrange - Create user without preferences
        var user = new User
        {
            Id = 2,
            Email = "noprefs@example.com",
            PasswordHash = "hash",
            CreatedDate = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var currentUserService = CreateCurrentUserService(2);

        // Act
        var result = await _query.GetUserPreferences(_context, currentUserService);

        // Assert
        result.Should().BeNull();
    }
}
