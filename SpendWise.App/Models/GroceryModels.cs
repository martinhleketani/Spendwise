namespace SpendWise.App.Models;


// ==========================================
// CREATE GROCERY LIST
// ==========================================

public class CreateGroceryListRequest
{
    public string ListName { get; set; } =
        string.Empty;

    public string Retailer { get; set; } =
        string.Empty;
}


public class CreateGroceryListResponse
{
    public string Message { get; set; } =
        string.Empty;

    public int Id { get; set; }

    public string ListName { get; set; } =
        string.Empty;

    public string Retailer { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public DateTime CreatedAt { get; set; }
}


// ==========================================
// GROCERY LIST SUMMARY
// GET /api/Grocery
// ==========================================

public class GroceryListSummary
{
    public int Id { get; set; }

    public string ListName { get; set; } =
        string.Empty;

    public string Retailer { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public int ItemCount { get; set; }

    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}


// ==========================================
// GET ONE GROCERY LIST
// ==========================================

public class GroceryListResponse
{
    public int Id { get; set; }

    public string ListName { get; set; } =
        string.Empty;

    public string Retailer { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public int ItemCount { get; set; }

    public decimal Total { get; set; }

    public List<GroceryItemResponse> Items
    {
        get;
        set;
    } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}


// ==========================================
// GROCERY ITEM
// ==========================================

public class GroceryItemResponse
{
    public int Id { get; set; }

    public string ProductName { get; set; } =
        string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public bool IsPurchased { get; set; }

    public DateTime CreatedAt { get; set; }
}


// ==========================================
// ADD GROCERY ITEM
// ==========================================

public class AddGroceryItemRequest
{
    public string ProductName { get; set; } =
        string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}


public class AddGroceryItemResponse
{
    public string Message { get; set; } =
        string.Empty;

    public GroceryItemResponse Item { get; set; } =
        new();

    public decimal ListTotal { get; set; }
}


// ==========================================
// BUDGET CHECK
// ==========================================

public class GroceryBudgetCheckRequest
{
    public int Month { get; set; }

    public int Year { get; set; }
}


public class GroceryBudgetCheckResponse
{
    public int GroceryListId { get; set; }

    public string GroceryListName { get; set; } =
        string.Empty;

    public string Retailer { get; set; } =
        string.Empty;

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal GroceryBudgetLimit { get; set; }

    public decimal AlreadySpent { get; set; }

    public decimal AvailableBeforeList { get; set; }

    public decimal ListTotal { get; set; }

    public decimal RemainingAfterList { get; set; }

    public bool WithinBudget { get; set; }

    public decimal OverBudgetBy { get; set; }

    public string Status { get; set; } =
        string.Empty;
}


// ==========================================
// CHECKOUT
// ==========================================

public class GroceryCheckoutRequest
{
    public int Month { get; set; }

    public int Year { get; set; }
}


public class GroceryCheckoutResponse
{
    public string Message { get; set; } =
        string.Empty;

    public int GroceryListId { get; set; }

    public string GroceryListName { get; set; } =
        string.Empty;

    public string Retailer { get; set; } =
        string.Empty;

    public decimal PurchaseTotal { get; set; }

    public decimal WalletBalance { get; set; }

    public int WalletVersion { get; set; }

    public decimal GroceryBudgetLimit { get; set; }

    public decimal GrocerySpent { get; set; }

    public decimal GroceryBudgetRemaining
    {
        get;
        set;
    }

    public bool WithinBudget { get; set; }

    public string BudgetStatus { get; set; } =
        string.Empty;

    public string TransactionReference
    {
        get;
        set;
    } = string.Empty;

    public string Status { get; set; } =
        string.Empty;
}


// ==========================================
// SIMPLE API MESSAGE
// Used when deleting an item/list
// ==========================================

public class GroceryMessageResponse
{
    public string Message { get; set; } =
        string.Empty;
}