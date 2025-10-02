namespace ChoreTracker.API.GraphQL.Types;

public class ChoreSkipAnalytics
{
    public int ChoreId { get; set; }
    public string ChoreName { get; set; } = string.Empty;
    public int TotalSkips { get; set; }
    public int TotalCompletions { get; set; }
    public double SkipRate { get; set; }
    public double? AverageNecessityAfterSkip { get; set; }
    public double? AverageNecessityOnTime { get; set; }
    public double? NecessityDifference { get; set; }
    public string? FrequencyRecommendation { get; set; }
}
