namespace SpendWise.App.Models;

public class RetailerPriceComparisonResponse
{
    public int GroceryListId { get; set; }

    public string GroceryListName { get; set; } =
        string.Empty;

    public List<RetailerComparisonResponse> Retailers { get; set; } =
        new List<RetailerComparisonResponse>();
}


public class RetailerComparisonResponse
{
    public int RetailerId { get; set; }

    public string RetailerName { get; set; } =
        string.Empty;

    public decimal EstimatedTotal { get; set; }

    public int MatchedItemCount { get; set; }

    public int MissingItemCount { get; set; }

    public bool HasCompletePricing { get; set; }

    public DateTime? LatestPriceCheckedAt { get; set; }

    public List<RetailerComparisonItemResponse> Items { get; set; } =
        new List<RetailerComparisonItemResponse>();
}


public class RetailerComparisonItemResponse
{
    public string ProductName { get; set; } =
        string.Empty;

    public int Quantity { get; set; }

    public bool PriceFound { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? MatchedProductName { get; set; }

    public string? Brand { get; set; }

    public string? Size { get; set; }

    public string? ProductUrl { get; set; }

    public DateTime? PriceCheckedAt { get; set; }
}