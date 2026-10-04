using SpendWise.API.Models;

namespace SpendWise.API.Services.Retailers
{
    // =========================================================
    // RETAILER PRICE PROVIDER
    // =========================================================
    //
    // Every online retailer integration used by SpendWise
    // must implement this interface.
    //
    // Examples:
    //
    // PickNPayPriceProvider
    // CheckersPriceProvider
    // ShopritePriceProvider
    //
    // This allows PriceComparisonService to work with all
    // retailers through the same contract.
    // =========================================================

    public interface IRetailerPriceProvider
    {
        // =====================================================
        // RETAILER NAME
        // =====================================================
        //
        // Examples:
        // "Pick n Pay"
        // "Checkers"
        // "Shoprite"
        // =====================================================

        string RetailerName { get; }


        // =====================================================
        // SEARCH PRODUCT
        // =====================================================
        //
        // Searches the retailer's online source for a product.
        //
        // If a reliable current price is found:
        //
        //     return RetailerProductResult
        //
        // If no reliable price can be obtained:
        //
        //     return null
        //
        // SpendWise must NOT generate or guess a price.
        // =====================================================

        Task<RetailerProductResult?> SearchProductAsync(
            string productName,
            CancellationToken cancellationToken = default);
    }
}