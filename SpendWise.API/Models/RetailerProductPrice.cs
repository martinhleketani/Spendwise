namespace SpendWise.API.Models
{
    // =========================================================
    // RETAILER PRODUCT PRICE
    // =========================================================
    //
    // Stores a retailer product price that SpendWise has
    // retrieved from a retailer source.
    //
    // IMPORTANT:
    //
    // This table must NOT contain randomly generated or fake
    // prices presented as current retailer prices.
    //
    // It can be used as a cache/history source for prices
    // retrieved by the retailer provider system.
    // =========================================================

    public class RetailerProductPrice
    {
        // =====================================================
        // PRIMARY KEY
        // =====================================================

        public int Id { get; set; }


        // =====================================================
        // RETAILER FOREIGN KEY
        // =====================================================

        public int RetailerId { get; set; }


        // =====================================================
        // RETAILER NAVIGATION PROPERTY
        // =====================================================

        public Retailer Retailer { get; set; } =
            null!;


        // =====================================================
        // PRODUCT NAME
        // =====================================================
        //
        // Actual product name returned by the retailer.
        // =====================================================

        public string ProductName { get; set; } =
            string.Empty;


        // =====================================================
        // BRAND
        // =====================================================

        public string? Brand { get; set; }


        // =====================================================
        // CATEGORY
        // =====================================================

        public string? Category { get; set; }


        // =====================================================
        // PRODUCT SIZE
        // =====================================================
        //
        // Examples:
        // 2L
        // 1kg
        // 500g
        // =====================================================

        public string? Size { get; set; }


        // =====================================================
        // CURRENT PRICE
        // =====================================================

        public decimal Price { get; set; }


        // =====================================================
        // REGULAR PRICE
        // =====================================================
        //
        // If a promotion exists, this can contain the original
        // price before the discount.
        // =====================================================

        public decimal? RegularPrice { get; set; }


        // =====================================================
        // PROMOTION STATUS
        // =====================================================

        public bool IsOnPromotion { get; set; }


        // =====================================================
        // AVAILABILITY
        // =====================================================

        public bool IsAvailable { get; set; } =
            true;


        // =====================================================
        // PRODUCT URL
        // =====================================================
        //
        // URL of the retailer product where available.
        // =====================================================

        public string? ProductUrl { get; set; }


        // =====================================================
        // PRICE SOURCE
        // =====================================================
        //
        // Identifies where SpendWise obtained the price.
        //
        // Examples:
        // Pick n Pay Online
        // Checkers Online
        // Shoprite Online
        // =====================================================

        public string? PriceSource { get; set; }


        // =====================================================
        // PRICE CHECKED DATE
        // =====================================================
        //
        // When SpendWise last successfully retrieved this
        // price from the retailer.
        // =====================================================

        public DateTime PriceCheckedAt { get; set; } =
            DateTime.UtcNow;


        // =====================================================
        // CREATED DATE
        // =====================================================

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;


        // =====================================================
        // UPDATED DATE
        // =====================================================

        public DateTime UpdatedAt { get; set; } =
            DateTime.UtcNow;
    }
}