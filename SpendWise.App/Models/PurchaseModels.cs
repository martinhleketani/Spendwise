namespace SpendWise.App.Models;


// ==========================================
// CREATE PURCHASE
// ==========================================

public class CreatePurchaseRequest
{
    public string Category { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public decimal Amount { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public string RequestId { get; set; } =
        string.Empty;
}


// ==========================================
// CREATE PURCHASE RESPONSE
// ==========================================

public class CreatePurchaseResponse
{
    public string Message { get; set; } =
        string.Empty;

    public bool Duplicate { get; set; }

    public int TransactionId { get; set; }

    public string? RequestId { get; set; }

    public string? Category { get; set; }

    public string? Description { get; set; }

    public decimal PurchaseAmount { get; set; }

    public decimal WalletBalance { get; set; }

    public int WalletVersion { get; set; }

    public decimal CategoryBudget { get; set; }

    public decimal CategorySpent { get; set; }

    public decimal CategoryRemaining { get; set; }

    public bool WithinBudget { get; set; }

    public string? BudgetStatus { get; set; }

    public string? TransactionReference { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    // Duplicate response fields
    public decimal Amount { get; set; }

    public DateTime? CreatedAt { get; set; }
}


// ==========================================
// PURCHASE HISTORY
// ==========================================

public class PurchaseHistoryResponse
{
    public int Count { get; set; }

    public decimal TotalAmount { get; set; }

    public PurchaseFilters Filters { get; set; } =
        new();

    public List<PurchaseHistoryItem> Purchases
    {
        get;
        set;
    } = new();
}


// ==========================================
// FILTERS
// ==========================================

public class PurchaseFilters
{
    public string? Category { get; set; }

    public string? Search { get; set; }

    public int? Month { get; set; }

    public int? Year { get; set; }

    public decimal? MinAmount { get; set; }

    public decimal? MaxAmount { get; set; }
}


// ==========================================
// PURCHASE HISTORY ITEM
// ==========================================

public class PurchaseHistoryItem
{
    public int Id { get; set; }

    public string? Category { get; set; }

    public decimal Amount { get; set; }

    public string Type { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public string Reference { get; set; } =
        string.Empty;

    public string? RequestId { get; set; }

    public DateTime CreatedAt { get; set; }
}