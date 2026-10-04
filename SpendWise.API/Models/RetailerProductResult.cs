namespace SpendWise.API.Models
{
    // =========================================================
    // RETAILER PRODUCT RESULT
    // =========================================================
    //
    // Represents a product result returned by a live retailer
    // price provider.
    //
    // This is NOT a database entity.
    // It is used while retrieving and comparing retailer prices.
    // =========================================================

    public class RetailerProductResult
    {
        // =====================================================
        // RETAILER
        // =====================================================

        public string RetailerName { get; set; } =
            string.Empty;


        // =====================================================
        // ORIGINAL SEARCH TERM
        // =====================================================

        public string SearchTerm { get; set; } =
            string.Empty;


        // =====================================================
        // PRODUCT NAME RETURNED BY RETAILER
        // =====================================================

        public string ProductName { get; set; } =
            string.Empty;


        // =====================================================
        // BRAND
        // =====================================================

        public string? Brand { get; set; }


        // =====================================================
        // PRODUCT SIZE
        // =====================================================

        public string? Size { get; set; }


        // =====================================================
        // CURRENT PRICE
        // =====================================================

        public decimal Price { get; set; }


        // =====================================================
        // REGULAR PRICE
        // =====================================================

        public decimal? RegularPrice { get; set; }


        // =====================================================
        // PROMOTION
        // =====================================================

        public bool IsOnPromotion { get; set; }


        // =====================================================
        // AVAILABILITY
        // =====================================================

        public bool IsAvailable { get; set; }


        // =====================================================
        // PRODUCT URL
        // =====================================================

        public string? ProductUrl { get; set; }


        // =====================================================
        // PRICE CHECKED
        // =====================================================

        public DateTime PriceCheckedAt { get; set; } =
            DateTime.UtcNow;


        // =====================================================
        // PRICE SOURCE
        // =====================================================

        public string PriceSource { get; set; } =
            string.Empty;
    }
}