using ChoreTracker.API.Models;

namespace ChoreTracker.API.GraphQL.Types;

public class RecurrencePatternType
{
    public int? DayInterval { get; set; }
    public int? WeekInterval { get; set; }
    public List<DayOfWeek>? DaysOfWeek { get; set; }
    public int? MonthInterval { get; set; }
    public int? DayOfMonth { get; set; }
    public string? RelativePattern { get; set; }
    public DateTime? EndDate { get; set; }

    public static RecurrencePatternType? FromModel(RecurrencePattern? pattern)
    {
        if (pattern == null) return null;

        return new RecurrencePatternType
        {
            DayInterval = pattern.DayInterval,
            WeekInterval = pattern.WeekInterval,
            DaysOfWeek = pattern.DaysOfWeek,
            MonthInterval = pattern.MonthInterval,
            DayOfMonth = pattern.DayOfMonth,
            RelativePattern = pattern.RelativePattern,
            EndDate = pattern.EndDate
        };
    }

    public RecurrencePattern ToModel()
    {
        return new RecurrencePattern
        {
            DayInterval = DayInterval,
            WeekInterval = WeekInterval,
            DaysOfWeek = DaysOfWeek,
            MonthInterval = MonthInterval,
            DayOfMonth = DayOfMonth,
            RelativePattern = RelativePattern,
            EndDate = EndDate
        };
    }
}
