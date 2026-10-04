namespace SpendWise.API.Models
{
    // =========================================================
    // RETAILER
    // =========================================================
    //
    // Represents a retailer supported by SpendWise.
    //
    // Examples:
    // - Pick n Pay
    // - Shoprite
    // - Checkers
    // - SPAR
    // - Woolworths Food
    //
    // This IS an Entity Framework database entity.
    // =========================================================

    public class Retailer
    {
        // =====================================================
        // PRIMARY KEY
        // =====================================================

        public int Id { get; set; }


        // =====================================================
        // RETAILER NAME
        // =====================================================

        public string Name { get; set; } =
            string.Empty;


        // =====================================================
        // RETAILER WEBSITE
        // =====================================================

        public string? WebsiteUrl { get; set; }


        // =====================================================
        // ACTIVE STATUS
        // =====================================================
        //
        // Allows a retailer to be disabled without deleting it.
        // =====================================================

        public bool IsActive { get; set; } =
            true;


        // =====================================================
        // CREATED DATE
        // =====================================================

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;


        // =====================================================
        // PRODUCT PRICES
        // =====================================================
        //
        // Cached/stored product prices belonging to this
        // retailer.
        // =====================================================

        public ICollection<RetailerProductPrice> ProductPrices
        {
            get;
            set;
        } = new List<RetailerProductPrice>();
    }
}