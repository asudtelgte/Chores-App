using ChoreTracker.API.Models;
using ChoreTracker.API.GraphQL.Types;

namespace ChoreTracker.API.GraphQL.Types;

public class ChoreType
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public ChoreCategory Category { get; set; }
    public RecurrencePatternType? RecurrencePattern { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompletedForPeriod { get; set; }
    public List<ChoreCompletion> Completions { get; set; } = new();

    public static ChoreType FromModel(Chore chore)
    {
        return new ChoreType
        {
            Id = chore.Id,
            Name = chore.Name,
            Description = chore.Description,
            Location = chore.Location,
            Category = chore.Category,
            RecurrencePattern = RecurrencePatternType.FromModel(chore.RecurrencePattern),
            Deadline = chore.Deadline,
            CreatedDate = chore.CreatedDate,
            IsActive = chore.IsActive,
            Completions = chore.Completions.ToList()
        };
    }
}
