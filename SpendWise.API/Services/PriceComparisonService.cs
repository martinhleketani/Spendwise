using SpendWise.API.Models;
using SpendWise.API.Services;

namespace SpendWise.API.Services.Retailers
{
    // =========================================================
    // PRICE COMPARISON SERVICE
    // =========================================================
    //
    // Coordinates grocery-price comparisons for SpendWise.
    //
    // Current supported retailers:
    //
    // - Pick n Pay
    // - Checkers
    // - Shoprite
    //
    // Prices are retrieved through GroceryPriceApiService.
    //
    // IMPORTANT:
    //
    // SpendWise never generates, estimates or substitutes
    // retailer prices.
    //
    // If a reliable positive price cannot be retrieved,
    // the retailer result remains null.
    // =========================================================

    public class PriceComparisonService
    {
        private readonly GroceryPriceApiService
            _groceryPriceApiService;

        private readonly ILogger<PriceComparisonService>
            _logger;


        // =====================================================
        // SUPPORTED RETAILERS
        // =====================================================

        private static readonly string[] SupportedRetailers =
        {
            "Pick n Pay",
            "Checkers",
            "Shoprite"
        };


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public PriceComparisonService(
            GroceryPriceApiService groceryPriceApiService,
            ILogger<PriceComparisonService> logger)
        {
            _groceryPriceApiService =
                groceryPriceApiService;

            _logger =
                logger;
        }


        // =====================================================
        // GET AVAILABLE RETAILER NAMES
        // =====================================================

        public IReadOnlyList<string>
            GetAvailableRetailers()
        {
            return SupportedRetailers
                .OrderBy(
                    retailer =>
                        retailer)
                .ToList();
        }


        // =====================================================
        // SEARCH ONE PRODUCT AT ALL RETAILERS
        // =====================================================

        public async Task<
            Dictionary<string, RetailerProductResult?>>
            SearchProductAcrossRetailersAsync(
                string productName,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(
                    productName))
            {
                throw new ArgumentException(
                    "Product name is required.",
                    nameof(productName));
            }


            string searchTerm =
                productName.Trim();


            // =================================================
            // CREATE EMPTY RESULTS FIRST
            // =================================================
            //
            // This guarantees that all supported retailers
            // appear in the response even when the external
            // source has no price.
            // =================================================

            var results =
                SupportedRetailers
                    .ToDictionary(
                        retailer =>
                            retailer,

                        retailer =>
                            (RetailerProductResult?)null,

                        StringComparer.OrdinalIgnoreCase);


            try
            {
                // =============================================
                // ONE EXTERNAL SEARCH
                // =============================================
                //
                // GroceryPriceApiService performs one product
                // search and then separates the results into
                // Pick n Pay, Checkers and Shoprite.
                // =============================================

                Dictionary<
                    string,
                    RetailerProductResult?>
                    apiResults =
                        await _groceryPriceApiService
                            .SearchProductAcrossRetailersAsync(
                                searchTerm,
                                cancellationToken);


                // =============================================
                // VALIDATE EACH RETAILER RESULT
                // =============================================

                foreach (
                    string retailer
                    in SupportedRetailers)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();


                    if (!apiResults.TryGetValue(
                            retailer,
                            out RetailerProductResult?
                                product))
                    {
                        results[retailer] =
                            null;

                        continue;
                    }


                    // =========================================
                    // NO PRODUCT FOUND
                    // =========================================

                    if (product == null)
                    {
                        results[retailer] =
                            null;

                        continue;
                    }


                    // =========================================
                    // INVALID PRICE
                    // =========================================
                    //
                    // Zero and negative prices are never
                    // accepted as real retailer prices.
                    // =========================================

                    if (product.Price <= 0)
                    {
                        _logger.LogWarning(
                            "Retailer {Retailer} returned an invalid price for {Product}.",
                            retailer,
                            searchTerm);


                        results[retailer] =
                            null;

                        continue;
                    }


                    // =========================================
                    // PRODUCT NAME VALIDATION
                    // =========================================

                    if (string.IsNullOrWhiteSpace(
                            product.ProductName))
                    {
                        _logger.LogWarning(
                            "Retailer {Retailer} returned a price without a valid product name for {Product}.",
                            retailer,
                            searchTerm);


                        results[retailer] =
                            null;

                        continue;
                    }


                    // =========================================
                    // NORMALIZE RETAILER
                    // =========================================

                    product.RetailerName =
                        retailer;


                    // =========================================
                    // NORMALIZE SEARCH TERM
                    // =========================================

                    if (string.IsNullOrWhiteSpace(
                            product.SearchTerm))
                    {
                        product.SearchTerm =
                            searchTerm;
                    }


                    // =========================================
                    // TIMESTAMP
                    // =========================================

                    if (product.PriceCheckedAt ==
                        default)
                    {
                        product.PriceCheckedAt =
                            DateTime.UtcNow;
                    }


                    // =========================================
                    // PRICE SOURCE
                    // =========================================

                    if (string.IsNullOrWhiteSpace(
                            product.PriceSource))
                    {
                        product.PriceSource =
                            "Grocery Price API";
                    }


                    results[retailer] =
                        product;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // =============================================
                // EXTERNAL SOURCE FAILURE
                // =============================================
                //
                // A failure must not result in generated or
                // estimated prices.
                //
                // All retailers remain unavailable.
                // =============================================

                _logger.LogWarning(
                    exception,
                    "Unable to retrieve grocery prices for {Product}.",
                    searchTerm);
            }


            return results;
        }


        // =====================================================
        // SEARCH ONE PRODUCT AT ONE RETAILER
        // =====================================================

        public async Task<RetailerProductResult?>
            SearchProductAtRetailerAsync(
                string productName,
                string retailerName,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(
                    productName))
            {
                throw new ArgumentException(
                    "Product name is required.",
                    nameof(productName));
            }


            if (string.IsNullOrWhiteSpace(
                    retailerName))
            {
                throw new ArgumentException(
                    "Retailer name is required.",
                    nameof(retailerName));
            }


            string? supportedRetailer =
                SupportedRetailers
                    .FirstOrDefault(
                        retailer =>
                            retailer.Equals(
                                retailerName.Trim(),
                                StringComparison.OrdinalIgnoreCase));


            if (supportedRetailer == null)
            {
                return null;
            }


            Dictionary<
                string,
                RetailerProductResult?>
                results =
                    await SearchProductAcrossRetailersAsync(
                        productName,
                        cancellationToken);


            results.TryGetValue(
                supportedRetailer,
                out RetailerProductResult?
                    product);


            return product;
        }


        // =====================================================
        // COMPARE COMPLETE GROCERY LIST
        // =====================================================

        public async Task<
            Dictionary<
                string,
                List<RetailerProductResult?>>>
            CompareGroceryListAsync(
                IEnumerable<GroceryItem> groceryItems,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                groceryItems);


            List<GroceryItem> items =
                groceryItems
                    .Where(
                        item =>
                            !string.IsNullOrWhiteSpace(
                                item.ProductName))
                    .ToList();


            var comparison =
                new Dictionary<
                    string,
                    List<RetailerProductResult?>>(
                        StringComparer.OrdinalIgnoreCase);


            // =================================================
            // CREATE RESULT LIST FOR EACH RETAILER
            // =================================================

            foreach (
                string retailer
                in SupportedRetailers)
            {
                comparison[retailer] =
                    new List<
                        RetailerProductResult?>();
            }


            // =================================================
            // SEARCH EACH GROCERY ITEM
            // =================================================

            foreach (
                GroceryItem item
                in items)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();


                string searchTerm =
                    item.ProductName.Trim();


                Dictionary<
                    string,
                    RetailerProductResult?>
                    productResults =
                        await SearchProductAcrossRetailersAsync(
                            searchTerm,
                            cancellationToken);


                // =============================================
                // ADD RESULT FOR EVERY RETAILER
                // =============================================

                foreach (
                    string retailer
                    in SupportedRetailers)
                {
                    productResults.TryGetValue(
                        retailer,
                        out RetailerProductResult?
                            retailerProduct);


                    comparison[retailer]
                        .Add(
                            retailerProduct);
                }
            }


            return comparison;
        }
    }
}