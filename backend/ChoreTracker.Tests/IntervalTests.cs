using ChoreTracker.API.Data;
using ChoreTracker.API.GraphQL.Inputs;
using ChoreTracker.API.GraphQL.Mutations;
using ChoreTracker.API.GraphQL.Types;
using ChoreTracker.API.Models;
using ChoreTracker.API.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ChoreTracker.Tests;

public class IntervalTests : IDisposable
{
    private readonly ChoreTrackerDbContext _context;
    private readonly Mutation _mutation;

    public IntervalTests()
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

        _context.Users.Add(user);
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
    public async Task CreateChore_WithWeekInterval_ShouldStoreCorrectly()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new ChoreInput
        {
            Name = "Bi-Weekly Chore",
            Category = ChoreCategory.Weekly,
            RecurrencePattern = new RecurrencePatternType
            {
                WeekInterval = 2,
                DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday }
            }
        };

        // Act
        var result = await _mutation.CreateChore(input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Bi-Weekly Chore");
        result.RecurrencePattern.Should().NotBeNull();
        result.RecurrencePattern!.WeekInterval.Should().Be(2);
        result.RecurrencePattern.DaysOfWeek.Should().Contain(DayOfWeek.Monday);
    }

    [Fact]
    public async Task CreateChore_WithMonthInterval_ShouldStoreCorrectly()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var input = new ChoreInput
        {
            Name = "Quarterly Chore",
            Category = ChoreCategory.Monthly,
            RecurrencePattern = new RecurrencePatternType
            {
                MonthInterval = 3,
                DayOfMonth = 1
            }
        };

        // Act
        var result = await _mutation.CreateChore(input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Quarterly Chore");
        result.RecurrencePattern.Should().NotBeNull();
        result.RecurrencePattern!.MonthInterval.Should().Be(3);
        result.RecurrencePattern.DayOfMonth.Should().Be(1);
    }

    [Fact]
    public async Task CreateChore_WithSixMonthInterval_ForFurnaceFilter()
    {
        // Arrange - simulating the furnace filter use case
        var currentUserService = CreateCurrentUserService(1);
        var input = new ChoreInput
        {
            Name = "Change Furnace Filter",
            Description = "Replace HVAC furnace filter",
            Category = ChoreCategory.Monthly,
            RecurrencePattern = new RecurrencePatternType
            {
                MonthInterval = 6,
                DayOfMonth = 1
            }
        };

        // Act
        var result = await _mutation.CreateChore(input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Change Furnace Filter");
        result.RecurrencePattern.Should().NotBeNull();
        result.RecurrencePattern!.MonthInterval.Should().Be(6);
    }

    [Fact]
    public async Task CreateChore_WithEveryThirtyDays_ForHeartwormMeds()
    {
        // Arrange - simulating the dog heartworm meds use case
        var currentUserService = CreateCurrentUserService(1);
        var input = new ChoreInput
        {
            Name = "Give Dog Heartworm Meds",
            Description = "Monthly heartworm prevention",
            Category = ChoreCategory.Daily,
            RecurrencePattern = new RecurrencePatternType
            {
                DayInterval = 30
            }
        };

        // Act
        var result = await _mutation.CreateChore(input, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Give Dog Heartworm Meds");
        result.RecurrencePattern.Should().NotBeNull();
        result.RecurrencePattern!.DayInterval.Should().Be(30);
    }

    [Fact]
    public async Task UpdateChore_WithWeekInterval_ShouldUpdateCorrectly()
    {
        // Arrange
        var currentUserService = CreateCurrentUserService(1);
        var createInput = new ChoreInput
        {
            Name = "Test Chore",
            Category = ChoreCategory.Weekly,
            RecurrencePattern = new RecurrencePatternType
            {
                WeekInterval = 1
            }
        };

        var created = await _mutation.CreateChore(createInput, _context, currentUserService);

        var updateInput = new ChoreInput
        {
            Name = "Test Chore",
            Category = ChoreCategory.Weekly,
            RecurrencePattern = new RecurrencePatternType
            {
                WeekInterval = 3
            }
        };

        // Act
        var result = await _mutation.UpdateChore(created.Id, updateInput, _context, currentUserService);

        // Assert
        result.Should().NotBeNull();
        result!.RecurrencePattern.Should().NotBeNull();
        result.RecurrencePattern!.WeekInterval.Should().Be(3);
    }
}
