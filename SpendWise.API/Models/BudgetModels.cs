namespace SpendWise.App.Models;

// ==========================================
// CREATE BUDGET REQUEST
// ==========================================

public class CreateBudgetRequest
{
    public int Month { get; set; }

    public int Year { get; set; }

    public decimal TotalLimit { get; set; }

    public List<CreateBudgetCategoryRequest> Categories { get; set; }
        = new();
}

public class CreateBudgetCategoryRequest
{
    public string Category { get; set; } = string.Empty;

    public decimal LimitAmount { get; set; }
}


// ==========================================
// CREATE BUDGET RESPONSE
// ==========================================

public class CreateBudgetResponse
{
    public string Message { get; set; } = string.Empty;

    public int BudgetId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal TotalLimit { get; set; }

    public decimal CategoryTotal { get; set; }

    public decimal Unallocated { get; set; }
}


// ==========================================
// GET BUDGET RESPONSE
// ==========================================

public class BudgetResponse
{
    public int Id { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal TotalLimit { get; set; }

    public decimal TotalSpent { get; set; }

    public decimal Remaining { get; set; }

    public List<BudgetCategoryResponse> Categories { get; set; }
        = new();
}

public class BudgetCategoryResponse
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public decimal Limit { get; set; }

    public decimal Spent { get; set; }

    public decimal Remaining { get; set; }
}


// ==========================================
// BUDGET ALERT RESPONSE
// ==========================================

public class BudgetAlertsResponse
{
    public int Month { get; set; }

    public int Year { get; set; }

    public BudgetAlertSummary Summary { get; set; } = new();

    public List<BudgetAlertItem> Alerts { get; set; }
        = new();
}

public class BudgetAlertSummary
{
    public decimal TotalBudget { get; set; }

    public decimal TotalSpent { get; set; }

    public decimal Remaining { get; set; }

    public decimal PercentageUsed { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class BudgetAlertItem
{
    public string Category { get; set; } = string.Empty;

    public decimal Limit { get; set; }

    public decimal Spent { get; set; }

    public decimal Remaining { get; set; }

    public decimal PercentageUsed { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}