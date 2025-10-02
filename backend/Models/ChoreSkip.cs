namespace ChoreTracker.API.Models;

public class ChoreSkip
{
    public int Id { get; set; }
    public int ChoreId { get; set; }
    public int UserId { get; set; }
    public DateTime SkippedDeadline { get; set; }
    public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }

    // Navigation properties
    public Chore Chore { get; set; } = null!;
    public User User { get; set; } = null!;
}
