namespace SpendWise.App.Models;

public class TransactionResponse
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public string Type { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Retailer { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? Reference { get; set; }

    public string? RequestId { get; set; }

    public DateTime CreatedAt { get; set; }
}