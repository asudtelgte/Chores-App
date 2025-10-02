using ChoreTracker.API.Data;
using ChoreTracker.API.GraphQL.Inputs;
using ChoreTracker.API.GraphQL.Types;
using ChoreTracker.API.Models;
using ChoreTracker.API.Services;
using Microsoft.EntityFrameworkCore;

namespace ChoreTracker.API.GraphQL.Mutations;

public class Mutation
{
    // ========== AUTHENTICATION MUTATIONS ==========

    public async Task<AuthPayload> Register(
        RegisterInput input,
        [Service] ChoreTrackerDbContext context,
        [Service] IAuthService authService)
    {
        // Check if email already exists
        if (await context.Users.AnyAsync(u => u.Email == input.Email))
        {
            throw new GraphQLException("Email already exists");
        }

        // Validate password strength
        if (input.Password.Length < 8)
        {
            throw new GraphQLException("Password must be at least 8 characters long");
        }

        if (!input.Password.Any(char.IsUpper))
        {
            throw new GraphQLException("Password must contain at least one uppercase letter");
        }

        if (!input.Password.Any(char.IsLower))
        {
            throw new GraphQLException("Password must contain at least one lowercase letter");
        }

        if (!input.Password.Any(char.IsDigit))
        {
            throw new GraphQLException("Password must contain at least one number");
        }

        if (!input.Password.Any(c => !char.IsLetterOrDigit(c)))
        {
            throw new GraphQLException("Password must contain at least one special character");
        }

        // Create new user
        var user = new User
        {
            Email = input.Email,
            PasswordHash = authService.HashPassword(input.Password),
            CreatedDate = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Generate JWT token
        var token = authService.GenerateJwtToken(user.Id, user.Email);

        return new AuthPayload
        {
            Token = token,
            User = user
        };
    }

    public async Task<AuthPayload> Login(
        LoginInput input,
        [Service] ChoreTrackerDbContext context,
        [Service] IAuthService authService)
    {
        // Find user by email
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == input.Email);

        if (user == null)
        {
            throw new GraphQLException("Invalid credentials");
        }

        // Verify password
        if (!authService.VerifyPassword(input.Password, user.PasswordHash))
        {
            throw new GraphQLException("Invalid credentials");
        }

        // Update last login date
        user.LastLoginDate = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Generate JWT token
        var token = authService.GenerateJwtToken(user.Id, user.Email);

        return new AuthPayload
        {
            Token = token,
            User = user
        };
    }

    // ========== CHORE MUTATIONS ==========

    public async Task<Chore> CreateChore(
        ChoreInput input,
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        var chore = new Chore
        {
            Name = input.Name,
            Description = input.Description,
            Location = input.Location,
            Category = input.Category,
            RecurrencePattern = input.RecurrencePattern?.ToModel(),
            Deadline = input.Deadline,
            CreatedDate = DateTime.UtcNow,
            IsActive = true,
            UserId = userId
        };

        context.Chores.Add(chore);
        await context.SaveChangesAsync();

        return chore;
    }

    public async Task<Chore?> UpdateChore(
        int id,
        ChoreInput input,
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        var chore = await context.Chores.FindAsync(id);
        if (chore == null)
            throw new GraphQLException("Chore not found");

        if (chore.UserId != userId)
            throw new GraphQLException("You do not have permission to update this chore");

        chore.Name = input.Name;
        chore.Description = input.Description;
        chore.Location = input.Location;
        chore.Category = input.Category;
        chore.RecurrencePattern = input.RecurrencePattern?.ToModel();
        chore.Deadline = input.Deadline;

        await context.SaveChangesAsync();

        return chore;
    }

    public async Task<bool> DeleteChore(
        int id,
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        var chore = await context.Chores.FindAsync(id);
        if (chore == null)
            throw new GraphQLException("Chore not found");

        if (chore.UserId != userId)
            throw new GraphQLException("You do not have permission to delete this chore");

        // Soft delete - set IsActive to false
        chore.IsActive = false;
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<ChoreCompletion> CompleteChore(
        CompleteChoreInput input,
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        // Validate necessity rating is in range 1-5
        if (input.NecessityRating < 1 || input.NecessityRating > 5)
        {
            throw new ArgumentException("Necessity rating must be between 1 and 5");
        }

        var chore = await context.Chores
            .Include(c => c.Completions)
            .Include(c => c.Skips)
            .FirstOrDefaultAsync(c => c.Id == input.ChoreId);

        if (chore == null)
        {
            throw new ArgumentException($"Chore with ID {input.ChoreId} not found");
        }

        if (chore.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only complete your own chores");
        }

        // Check if this completion is late (past deadline) and auto-record skips
        await AutoRecordSkippedDeadlines(chore, userId, context);

        var completion = new ChoreCompletion
        {
            ChoreId = input.ChoreId,
            CompletionDate = DateTime.UtcNow,
            NecessityRating = input.NecessityRating,
            Notes = input.Notes,
            UserId = userId
        };

        context.ChoreCompletions.Add(completion);
        await context.SaveChangesAsync();

        // Load the chore relationship
        await context.Entry(completion).Reference(c => c.Chore).LoadAsync();

        return completion;
    }

    private async Task AutoRecordSkippedDeadlines(Chore chore, int userId, ChoreTrackerDbContext context)
    {
        // Calculate what the deadline was for this chore
        var lastCompletion = chore.Completions
            .OrderByDescending(c => c.CompletionDate)
            .FirstOrDefault();

        var baseDate = lastCompletion?.CompletionDate.Date ?? chore.CreatedDate.Date;
        var now = DateTime.UtcNow.Date;

        // Calculate all deadlines between last completion and now
        var missedDeadlines = new List<DateTime>();
        var endDate = chore.RecurrencePattern?.EndDate?.Date;

        switch (chore.Category)
        {
            case ChoreCategory.Daily:
                var dayInterval = chore.RecurrencePattern?.DayInterval ?? 1;
                var nextDeadline = baseDate.AddDays(dayInterval);
                while (nextDeadline < now)
                {
                    // Stop recording skips after the end date
                    if (endDate.HasValue && nextDeadline > endDate.Value)
                        break;

                    // Check if we already recorded this skip
                    var alreadySkipped = chore.Skips.Any(s => s.SkippedDeadline.Date == nextDeadline);
                    if (!alreadySkipped)
                    {
                        missedDeadlines.Add(nextDeadline);
                    }
                    nextDeadline = nextDeadline.AddDays(dayInterval);
                }
                break;

            case ChoreCategory.Weekly:
                var lastWeekStart = GetStartOfWeek(baseDate);
                var nextWeekEnd = lastWeekStart.AddDays(13); // End of next week
                while (nextWeekEnd < now)
                {
                    // Stop recording skips after the end date
                    if (endDate.HasValue && nextWeekEnd > endDate.Value)
                        break;

                    var alreadySkipped = chore.Skips.Any(s => s.SkippedDeadline.Date == nextWeekEnd);
                    if (!alreadySkipped)
                    {
                        missedDeadlines.Add(nextWeekEnd);
                    }
                    nextWeekEnd = nextWeekEnd.AddDays(7);
                }
                break;

            case ChoreCategory.Monthly:
                var nextMonthDeadline = CalculateNextMonthlyDeadline(baseDate, chore.RecurrencePattern);
                while (nextMonthDeadline.HasValue && nextMonthDeadline.Value < now)
                {
                    // Stop recording skips after the end date
                    if (endDate.HasValue && nextMonthDeadline.Value > endDate.Value)
                        break;

                    var alreadySkipped = chore.Skips.Any(s => s.SkippedDeadline.Date == nextMonthDeadline.Value);
                    if (!alreadySkipped)
                    {
                        missedDeadlines.Add(nextMonthDeadline.Value);
                    }
                    nextMonthDeadline = CalculateNextMonthlyDeadline(nextMonthDeadline.Value, chore.RecurrencePattern);
                }
                break;

            case ChoreCategory.Special:
                if (chore.Deadline.HasValue && chore.Deadline.Value < now)
                {
                    var alreadySkipped = chore.Skips.Any(s => s.SkippedDeadline.Date == chore.Deadline.Value.Date);
                    if (!alreadySkipped)
                    {
                        missedDeadlines.Add(chore.Deadline.Value);
                    }
                }
                break;
        }

        // Record all missed deadlines as skips
        foreach (var deadline in missedDeadlines)
        {
            var skip = new ChoreSkip
            {
                ChoreId = chore.Id,
                UserId = userId,
                SkippedDeadline = deadline,
                RecordedDate = DateTime.UtcNow,
                Reason = "Automatically recorded - deadline passed"
            };
            context.ChoreSkips.Add(skip);
        }

        if (missedDeadlines.Any())
        {
            await context.SaveChangesAsync();
        }
    }

    private DateTime GetStartOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private DateTime? CalculateNextMonthlyDeadline(DateTime baseDate, RecurrencePattern? pattern)
    {
        if (pattern?.DayOfMonth.HasValue == true)
        {
            var currentMonth = baseDate.Year * 12 + baseDate.Month;
            var nextMonth = currentMonth + 1;
            var nextYear = nextMonth / 12;
            var nextMonthNum = nextMonth % 12;
            if (nextMonthNum == 0)
            {
                nextMonthNum = 12;
                nextYear--;
            }

            var daysInMonth = DateTime.DaysInMonth(nextYear, nextMonthNum);
            var targetDay = Math.Min(pattern.DayOfMonth.Value, daysInMonth);
            return new DateTime(nextYear, nextMonthNum, targetDay);
        }

        var lastCompletionMonth = baseDate.Year * 12 + baseDate.Month;
        var nextMonthValue = lastCompletionMonth + 1;
        var yearForNext = nextMonthValue / 12;
        var monthForNext = nextMonthValue % 12;
        if (monthForNext == 0)
        {
            monthForNext = 12;
            yearForNext--;
        }

        var lastDayOfNextMonth = DateTime.DaysInMonth(yearForNext, monthForNext);
        return new DateTime(yearForNext, monthForNext, lastDayOfNextMonth);
    }

    public async Task<ChoreSkip> SkipChore(
        SkipChoreInput input,
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        var chore = await context.Chores.FindAsync(input.ChoreId);
        if (chore == null)
        {
            throw new ArgumentException($"Chore with ID {input.ChoreId} not found");
        }

        if (chore.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only skip your own chores");
        }

        var skip = new ChoreSkip
        {
            ChoreId = input.ChoreId,
            UserId = userId,
            SkippedDeadline = input.SkippedDeadline,
            RecordedDate = DateTime.UtcNow,
            Reason = input.Reason ?? "Manually skipped"
        };

        context.ChoreSkips.Add(skip);
        await context.SaveChangesAsync();

        // Load the chore relationship
        await context.Entry(skip).Reference(s => s.Chore).LoadAsync();

        return skip;
    }

    public async Task<ChoreCompletion?> UpdateCompletionRating(
        int completionId,
        int rating,
        string? notes,
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        // Validate necessity rating is in range 1-5
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentException("Necessity rating must be between 1 and 5");
        }

        var completion = await context.ChoreCompletions.FindAsync(completionId);
        if (completion == null || completion.UserId != userId)
            return null;

        completion.NecessityRating = rating;
        completion.Notes = notes;

        await context.SaveChangesAsync();

        return completion;
    }

    public async Task<UserPreferences> UpdateUserPreferences(
        int? dayStartHour,
        string? theme,
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        // Validate hour is 0-23 if provided
        if (dayStartHour.HasValue && (dayStartHour < 0 || dayStartHour > 23))
        {
            throw new ArgumentException("Day start hour must be between 0 and 23");
        }

        var prefs = await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs == null)
        {
            // Create new preferences
            prefs = new UserPreferences
            {
                DayStartHour = dayStartHour ?? 0,
                Theme = theme ?? "yasified",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                UserId = userId
            };
            context.UserPreferences.Add(prefs);
        }
        else
        {
            // Update existing (only update provided fields)
            if (dayStartHour.HasValue)
                prefs.DayStartHour = dayStartHour.Value;
            if (theme != null)
                prefs.Theme = theme;
            prefs.ModifiedDate = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return prefs;
    }
}
