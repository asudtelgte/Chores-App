namespace ChoreTracker.API.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }

    // Navigation properties - a user has many chores, completions, skips, and preferences
    public ICollection<Chore> Chores { get; set; } = new List<Chore>();
    public ICollection<ChoreCompletion> Completions { get; set; } = new List<ChoreCompletion>();
    public ICollection<ChoreSkip> Skips { get; set; } = new List<ChoreSkip>();
    public UserPreferences? Preferences { get; set; }
}
