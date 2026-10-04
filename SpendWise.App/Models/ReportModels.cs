namespace SpendWise.App.Models;


// =====================================================
// MONTHLY REPORT RESPONSE
// =====================================================

public class MonthlyReportResponse
{
    public int Month { get; set; }

    public int Year { get; set; }

    public MonthlyReportSummary Summary { get; set; } =
        new();

    public List<ReportCategoryItem> Categories { get; set; } =
        new();

    public BiggestTransactionResponse? BiggestTransaction { get; set; }
}


// =====================================================
// MONTHLY REPORT SUMMARY
// =====================================================

public class MonthlyReportSummary
{
    public decimal TotalSpent { get; set; }

    public int TransactionCount { get; set; }

    public decimal AveragePurchase { get; set; }

    public decimal BiggestPurchase { get; set; }
}


// =====================================================
// BIGGEST TRANSACTION
// =====================================================

public class BiggestTransactionResponse
{
    public int Id { get; set; }

    public string? Category { get; set; }

    public string? Retailer { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}


// =====================================================
// REPORT CATEGORY ITEM
// =====================================================

public class ReportCategoryItem
{
    public string Category { get; set; } =
        string.Empty;

    public decimal Amount { get; set; }

    public decimal Percentage { get; set; }

    public int TransactionCount { get; set; }
}


// =====================================================
// RETAILER REPORT RESPONSE
// =====================================================

public class RetailerReportResponse
{
    public int Month { get; set; }

    public int Year { get; set; }

    public RetailerReportSummary Summary { get; set; } =
        new();

    public List<ReportRetailerItem> Retailers { get; set; } =
        new();

    public ReportRetailerItem? HighestSpendingRetailer { get; set; }
}


// =====================================================
// RETAILER REPORT SUMMARY
// =====================================================

public class RetailerReportSummary
{
    public decimal TotalSpentAtRetailers { get; set; }

    public int TransactionCount { get; set; }

    public decimal AverageRetailerPurchase { get; set; }

    public int RetailerCount { get; set; }
}


// =====================================================
// REPORT RETAILER ITEM
// =====================================================

public class ReportRetailerItem
{
    public string Retailer { get; set; } =
        string.Empty;

    public decimal Amount { get; set; }

    public decimal Percentage { get; set; }

    public int TransactionCount { get; set; }
}


// =====================================================
// DATE RANGE REPORT RESPONSE
// =====================================================

public class DateRangeReportResponse
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateRangeReportSummary Summary { get; set; } =
        new();

    public List<ReportCategoryItem> Categories { get; set; } =
        new();

    public List<ReportRetailerItem> Retailers { get; set; } =
        new();

    public List<ReportTransactionItem> Transactions { get; set; } =
        new();
}


// =====================================================
// DATE RANGE REPORT SUMMARY
// =====================================================

public class DateRangeReportSummary
{
    public decimal TotalSpent { get; set; }

    public int TransactionCount { get; set; }

    public decimal AveragePurchase { get; set; }

    public decimal BiggestPurchase { get; set; }
}


// =====================================================
// REPORT TRANSACTION ITEM
// =====================================================

public class ReportTransactionItem
{
    public int Id { get; set; }

    public string? Category { get; set; }

    public string? Retailer { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}