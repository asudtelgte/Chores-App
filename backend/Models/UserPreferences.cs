namespace ChoreTracker.API.Models;

public class UserPreferences
{
    public int Id { get; set; }
    public int DayStartHour { get; set; } = 0; // Default: midnight (0-23)
    public string Theme { get; set; } = "yasified"; // Default theme
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Foreign key to User (one-to-one relationship)
    public int UserId { get; set; }

    // Navigation property
    public User? User { get; set; }
}
