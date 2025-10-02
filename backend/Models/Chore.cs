namespace ChoreTracker.API.Models;

public class Chore
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; } // Where this chore takes place
    public ChoreCategory Category { get; set; }
    public RecurrencePattern? RecurrencePattern { get; set; }
    public DateTime? Deadline { get; set; } // For special chores or overrides
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Foreign key to User
    public int UserId { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<ChoreCompletion> Completions { get; set; } = new List<ChoreCompletion>();
    public ICollection<ChoreSkip> Skips { get; set; } = new List<ChoreSkip>();
}
