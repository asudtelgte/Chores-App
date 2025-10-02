namespace ChoreTracker.API.Models;

public class ChoreCompletion
{
    public int Id { get; set; }
    public int ChoreId { get; set; }
    public DateTime CompletionDate { get; set; } = DateTime.UtcNow;
    public int NecessityRating { get; set; } // 1-5 scale
    public string? Notes { get; set; }

    // Foreign key to User
    public int UserId { get; set; }

    // Navigation properties
    public Chore? Chore { get; set; }
    public User? User { get; set; }
}
