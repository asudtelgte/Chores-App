namespace ChoreTracker.API.GraphQL.Inputs;

public class CompleteChoreInput
{
    public int ChoreId { get; set; }
    public int NecessityRating { get; set; } // 1-5
    public string? Notes { get; set; }
}
