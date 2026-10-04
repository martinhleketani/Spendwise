namespace SpendWise.App.Models;


// =====================================================
// DASHBOARD RESPONSE
// =====================================================

public class DashboardResponse
{
    public int Month { get; set; }

    public int Year { get; set; }

    public DashboardMonthlyBudget MonthlyBudget { get; set; } = new();

    public DashboardGrocerySummary? Groceries { get; set; }

    public List<DashboardCategory> Categories { get; set; } = new();

    public List<DashboardTransaction> RecentTransactions { get; set; } = new();

    public List<DashboardGroceryList> RecentGroceryLists { get; set; } = new();
}


// =====================================================
// MONTHLY BUDGET
// =====================================================

public class DashboardMonthlyBudget
{
    public decimal Limit { get; set; }

    public decimal Spent { get; set; }

    public decimal Remaining { get; set; }

    public decimal PercentageUsed { get; set; }
}


// =====================================================
// GROCERY SUMMARY
// =====================================================

public class DashboardGrocerySummary
{
    public decimal Limit { get; set; }

    public decimal Spent { get; set; }

    public decimal Remaining { get; set; }

    public decimal PercentageUsed { get; set; }
}


// =====================================================
// CATEGORY
// =====================================================

public class DashboardCategory
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public decimal Limit { get; set; }

    public decimal Spent { get; set; }

    public decimal Remaining { get; set; }

    public decimal PercentageUsed { get; set; }
}


// =====================================================
// RECENT SPENDING TRANSACTION
// =====================================================

public class DashboardTransaction
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Reference { get; set; }

    public DateTime CreatedAt { get; set; }
}


// =====================================================
// RECENT GROCERY LIST
// =====================================================

public class DashboardGroceryList
{
    public int Id { get; set; }

    public string ListName { get; set; } = string.Empty;

    public string? Retailer { get; set; }

    public string Status { get; set; } = string.Empty;

    public int ItemCount { get; set; }

    public decimal Total { get; set; }

    public DateTime UpdatedAt { get; set; }
}