namespace SpendWise.App.Models;


// =====================================================
// INSIGHTS RESPONSE
// =====================================================

public class InsightsResponse
{
    public int Month { get; set; }

    public int Year { get; set; }

    public InsightsSummary Summary { get; set; } =
        new();

    public List<InsightItem> Insights { get; set; } =
        new();

    public List<CategoryInsightItem> Categories { get; set; } =
        new();
}


// =====================================================
// INSIGHTS SUMMARY
// =====================================================

public class InsightsSummary
{
    public decimal TotalBudget { get; set; }

    public decimal TotalSpent { get; set; }

    public decimal TotalRemaining { get; set; }

    public decimal PercentageUsed { get; set; }
}


// =====================================================
// GENERAL INSIGHT
// =====================================================

public class InsightItem
{
    public string Type { get; set; } =
        string.Empty;

    public string Level { get; set; } =
        string.Empty;

    public string? Category { get; set; }

    public string Message { get; set; } =
        string.Empty;
}


// =====================================================
// CATEGORY INSIGHT
// =====================================================

public class CategoryInsightItem
{
    public string Category { get; set; } =
        string.Empty;

    public decimal Budget { get; set; }

    public decimal Spent { get; set; }

    public decimal Remaining { get; set; }

    public decimal PercentageUsed { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string Message { get; set; } =
        string.Empty;
}