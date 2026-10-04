namespace SpendWise.API.DTOs
{
    // =========================================================
    // RETAILER PRICE COMPARISON RESPONSE
    // =========================================================
    //
    // Returned by:
    // GET /api/Grocery/{id}/price-comparison
    // =========================================================

    public class RetailerPriceComparisonDto
    {
        public int GroceryListId { get; set; }

        public string GroceryListName { get; set; } =
            string.Empty;

        public DateTime GeneratedAt { get; set; } =
            DateTime.UtcNow;

        public List<RetailerComparisonDto> Retailers
        {
            get;
            set;
        } = new();
    }


    // =========================================================
    // ONE RETAILER'S BASKET COMPARISON
    // =========================================================

    public class RetailerComparisonDto
    {
        public int RetailerId { get; set; }

        public string RetailerName { get; set; } =
            string.Empty;

        public decimal EstimatedTotal { get; set; }

        public int MatchedItemCount { get; set; }

        public int MissingItemCount { get; set; }

        public bool HasCompletePricing { get; set; }

        public DateTime? LatestPriceCheckedAt { get; set; }

        public List<RetailerComparisonItemDto> Items
        {
            get;
            set;
        } = new();
    }


    // =========================================================
    // INDIVIDUAL PRODUCT COMPARISON
    // =========================================================

    public class RetailerComparisonItemDto
    {
        // Product requested by the SpendWise user.
        public string ProductName { get; set; } =
            string.Empty;

        public int Quantity { get; set; }

        // False when no reliable retailer price was found.
        public bool PriceFound { get; set; }


        // =====================================================
        // PRICE
        // =====================================================

        public decimal? UnitPrice { get; set; }

        public decimal? RegularPrice { get; set; }

        public decimal? TotalPrice { get; set; }

        public bool IsOnPromotion { get; set; }


        // =====================================================
        // ACTUAL MATCHED RETAILER PRODUCT
        // =====================================================

        public string? MatchedProductName { get; set; }

        public string? Brand { get; set; }

        public string? Size { get; set; }


        // =====================================================
        // PRICE VERIFICATION
        // =====================================================

        public string? ProductUrl { get; set; }

        public string? PriceSource { get; set; }

        public DateTime? PriceCheckedAt { get; set; }
    }
}