using ChoreTracker.API.GraphQL.Types;
using ChoreTracker.API.Models;

namespace ChoreTracker.API.GraphQL.Inputs;

public class ChoreInput
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public ChoreCategory Category { get; set; }
    public RecurrencePatternType? RecurrencePattern { get; set; }
    public DateTime? Deadline { get; set; }
}
