namespace ChoreTracker.API.GraphQL.Inputs;

public class SkipChoreInput
{
    public required int ChoreId { get; set; }
    public DateTime SkippedDeadline { get; set; }
    public string? Reason { get; set; }
}
