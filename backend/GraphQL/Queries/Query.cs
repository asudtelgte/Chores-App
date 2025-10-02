using ChoreTracker.API.Data;
using ChoreTracker.API.Models;
using ChoreTracker.API.Services;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

namespace ChoreTracker.API.GraphQL.Queries;

public class Query
{
    public async Task<List<ChoreTracker.API.GraphQL.Types.ChoreType>> GetChores(
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        var chores = await context.Chores
            .Include(c => c.Completions)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        // Get user preferences for day start time (default to midnight if not set)
        var userPrefs = await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        int dayStartHour = userPrefs?.DayStartHour ?? 0;

        return chores.Select(c =>
        {
            var choreType = ChoreTracker.API.GraphQL.Types.ChoreType.FromModel(c);
            choreType.IsCompletedForPeriod = IsCompletedForCurrentPeriod(c, dayStartHour);
            return choreType;
        }).ToList();
    }

    public async Task<Chore?> GetChore(
        int id,
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        return await context.Chores
            .Include(c => c.Completions)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    }

    public async Task<List<Chore>> GetUpcomingChores(
        int daysAhead,
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();
        var today = DateTime.UtcNow.Date;
        var futureDate = today.AddDays(daysAhead);

        // Get all active chores with their last completion
        var chores = await context.Chores
            .Include(c => c.Completions)
            .Where(c => c.IsActive && c.UserId == userId)
            .ToListAsync();

        var upcomingChores = new List<Chore>();

        foreach (var chore in chores)
        {
            var nextDue = CalculateNextDueDate(chore);
            if (nextDue.HasValue && nextDue.Value <= futureDate)
            {
                upcomingChores.Add(chore);
            }
        }

        return upcomingChores.OrderBy(c => CalculateNextDueDate(c)).ToList();
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ChoreCompletion> GetCompletionHistory(
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();
        return context.ChoreCompletions
            .Include(cc => cc.Chore)
            .Where(cc => cc.UserId == userId);
    }

    public async Task<UserPreferences?> GetUserPreferences(
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();
        return await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<List<ChoreTracker.API.GraphQL.Types.ChoreSkipAnalytics>> GetChoreSkipAnalytics(
        [Service] ChoreTrackerDbContext context,
        [Service] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserIdOrThrow();

        var chores = await context.Chores
            .Include(c => c.Skips)
            .Include(c => c.Completions)
            .Where(c => c.UserId == userId && c.IsActive)
            .ToListAsync();

        var analytics = new List<ChoreTracker.API.GraphQL.Types.ChoreSkipAnalytics>();

        foreach (var chore in chores)
        {
            var totalSkips = chore.Skips.Count;
            var totalCompletions = chore.Completions.Count;

            if (totalCompletions == 0 && totalSkips == 0)
                continue; // Skip chores with no activity

            // Find completions that happened after a skip
            var completionsAfterSkip = chore.Completions
                .Where(c => chore.Skips.Any(s => s.SkippedDeadline < c.CompletionDate))
                .ToList();

            // Find completions that happened on time (no skips before them)
            var onTimeCompletions = chore.Completions
                .Where(c => !chore.Skips.Any(s => s.SkippedDeadline < c.CompletionDate &&
                    !chore.Completions.Any(prev => prev.CompletionDate > s.SkippedDeadline && prev.CompletionDate < c.CompletionDate)))
                .ToList();

            double? avgNecessityAfterSkip = completionsAfterSkip.Any()
                ? completionsAfterSkip.Average(c => c.NecessityRating)
                : null;

            double? avgNecessityOnTime = onTimeCompletions.Any()
                ? onTimeCompletions.Average(c => c.NecessityRating)
                : null;

            double? necessityDiff = null;
            string? recommendation = null;

            if (avgNecessityAfterSkip.HasValue && avgNecessityOnTime.HasValue)
            {
                necessityDiff = avgNecessityAfterSkip.Value - avgNecessityOnTime.Value;

                // If skipped chores have LOWER necessity when finally completed,
                // it suggests the frequency is too high
                if (necessityDiff < -0.5)
                {
                    recommendation = "Consider reducing frequency - necessity is lower after skipping";
                }
                else if (necessityDiff > 0.5)
                {
                    recommendation = "Current frequency seems appropriate - necessity increases when skipped";
                }
                else
                {
                    recommendation = "Frequency appears balanced";
                }
            }
            else if (totalSkips > totalCompletions && totalSkips > 2)
            {
                recommendation = "High skip rate - consider reducing frequency";
            }

            var skipRate = (totalCompletions + totalSkips) > 0
                ? (double)totalSkips / (totalSkips + totalCompletions)
                : 0;

            analytics.Add(new ChoreTracker.API.GraphQL.Types.ChoreSkipAnalytics
            {
                ChoreId = chore.Id,
                ChoreName = chore.Name,
                TotalSkips = totalSkips,
                TotalCompletions = totalCompletions,
                SkipRate = skipRate,
                AverageNecessityAfterSkip = avgNecessityAfterSkip,
                AverageNecessityOnTime = avgNecessityOnTime,
                NecessityDifference = necessityDiff,
                FrequencyRecommendation = recommendation
            });
        }

        return analytics.OrderByDescending(a => a.SkipRate).ToList();
    }

    private DateTime? CalculateNextDueDate(Chore chore)
    {
        if (chore.Category == ChoreCategory.Special)
        {
            return chore.Deadline;
        }

        var lastCompletion = chore.Completions
            .OrderByDescending(c => c.CompletionDate)
            .FirstOrDefault();

        var baseDate = lastCompletion?.CompletionDate.Date ?? chore.CreatedDate.Date;

        var nextDueDate = chore.Category switch
        {
            ChoreCategory.Daily => baseDate.AddDays(chore.RecurrencePattern?.DayInterval ?? 1),
            ChoreCategory.Weekly => CalculateNextWeeklyDueDate(baseDate),
            ChoreCategory.Monthly => CalculateNextMonthlyDate(baseDate, chore.RecurrencePattern),
            _ => null
        };

        // Check if the next due date is after the recurrence end date
        if (nextDueDate.HasValue &&
            chore.RecurrencePattern?.EndDate.HasValue == true &&
            nextDueDate.Value > chore.RecurrencePattern.EndDate.Value)
        {
            return null; // Recurrence has ended
        }

        return nextDueDate;
    }

    private DateTime? CalculateNextWeeklyDueDate(DateTime lastCompletionOrCreatedDate)
    {
        // Weekly chores need to be done once per week (Monday-Sunday)
        // Find the start of the week for the last completion
        var lastWeekStart = GetStartOfWeek(lastCompletionOrCreatedDate);

        // Next due date is the start of the following week (next Monday)
        var nextWeekStart = lastWeekStart.AddDays(7);

        // Return the end of that week (Sunday) as the deadline
        return nextWeekStart.AddDays(6);
    }

    private DateTime GetStartOfWeek(DateTime date)
    {
        // Get Monday of the week containing this date
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private DateTime? CalculateNextMonthlyDate(DateTime baseDate, RecurrencePattern? pattern)
    {
        // If a specific day of month is set, use that as the due date
        if (pattern?.DayOfMonth.HasValue == true)
        {
            // Find the next occurrence of this day
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

        // If no specific day is set, monthly chores just need to be done anytime during the next month
        // Return the last day of the next month as the deadline
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

    private bool IsCompletedForCurrentPeriod(Chore chore, int dayStartHour)
    {
        if (chore.Completions == null || !chore.Completions.Any())
            return false;

        var now = DateTime.UtcNow;
        var lastCompletion = chore.Completions
            .OrderByDescending(c => c.CompletionDate)
            .FirstOrDefault();

        if (lastCompletion == null)
            return false;

        return chore.Category switch
        {
            ChoreCategory.Daily => IsCompletedForToday(lastCompletion.CompletionDate, now, dayStartHour),
            ChoreCategory.Weekly => IsCompletedForThisWeek(lastCompletion.CompletionDate, now, dayStartHour),
            ChoreCategory.Monthly => IsCompletedForThisMonth(lastCompletion.CompletionDate, now),
            ChoreCategory.Special => lastCompletion.CompletionDate >= (chore.Deadline ?? DateTime.MaxValue),
            _ => false
        };
    }

    private bool IsCompletedForToday(DateTime completionDate, DateTime now, int dayStartHour)
    {
        // Adjust "now" based on day start hour (e.g., 3am means day starts at 3am)
        var adjustedNow = now.Hour < dayStartHour ? now.AddDays(-1) : now;
        var adjustedCompletion = completionDate.Hour < dayStartHour ? completionDate.AddDays(-1) : completionDate;

        return adjustedCompletion.Date == adjustedNow.Date;
    }

    private bool IsCompletedForThisWeek(DateTime completionDate, DateTime now, int dayStartHour)
    {
        // Adjust dates based on day start hour
        var adjustedNow = now.Hour < dayStartHour ? now.AddDays(-1) : now;
        var adjustedCompletion = completionDate.Hour < dayStartHour ? completionDate.AddDays(-1) : completionDate;

        var currentWeekStart = GetStartOfWeek(adjustedNow.Date);
        var completionWeekStart = GetStartOfWeek(adjustedCompletion.Date);

        return completionWeekStart == currentWeekStart;
    }

    private bool IsCompletedForThisMonth(DateTime completionDate, DateTime now)
    {
        return completionDate.Year == now.Year && completionDate.Month == now.Month;
    }
}
