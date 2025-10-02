namespace ChoreTracker.API.Models;

public class RecurrencePattern
{
    public int? DayInterval { get; set; } // For daily chores (e.g., every 1 day)
    public List<DayOfWeek>? DaysOfWeek { get; set; } // For weekly chores (e.g., Monday, Thursday)
    public int? DayOfMonth { get; set; } // For monthly chores (e.g., 15th of each month)
    public string? RelativePattern { get; set; } // For relative monthly (e.g., "First Monday", "Last Friday")
    public DateTime? EndDate { get; set; } // When this recurrence pattern should stop (for seasonal chores)
}
