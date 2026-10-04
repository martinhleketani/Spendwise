using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SpendWise.API.Models;

namespace SpendWise.API.Services
{
    public class GroceryPriceApiService
    {
        private readonly HttpClient _httpClient;
        private readonly GroceryPriceApiOptions _options;
        private readonly ILogger<GroceryPriceApiService> _logger;

        private static readonly string[] SupportedRetailers =
        {
            "Pick n Pay",
            "Checkers",
            "Shoprite"
        };

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        private const int MaximumPagesPerSearch = 20;

        // =====================================================
        // PRODUCT VARIANT TERMS
        // =====================================================
        //
        // These terms help SpendWise distinguish products that
        // share the same brand/name and size but are actually
        // different variants.
        //
        // Example:
        //
        // Coca-Cola Original 2L
        // Coca-Cola Zero Sugar 2L
        // Coca-Cola Zero Sugar Zero Caffeine 2L
        //
        // We do NOT want these to be treated as identical.
        // =====================================================

        private static readonly string[] VariantPhrases =
        {
            "zero sugar zero caffeine",
            "zero caffeine",
            "zero sugar",
            "no sugar",
            "sugar free",
            "less sugar",
            "reduced sugar",
            "original taste",
            "original",
            "diet",
            "light",
            "decaf",
            "decaffeinated",
            "caffeine free",
            "full cream",
            "full fat",
            "low fat",
            "fat free",
            "skimmed",
            "semi skimmed",
            "unsweetened",
            "sweetened",
            "reduced fat",
            "extra lean",
            "lean"
        };

        private static readonly string[] AlternativeVariantTerms =
        {
            "zero",
            "diet",
            "light",
            "decaf",
            "decaffeinated",
            "unsweetened",
            "sweetened",
            "skimmed"
        };

        public GroceryPriceApiService(
            HttpClient httpClient,
            IOptions<GroceryPriceApiOptions> options,
            ILogger<GroceryPriceApiService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                _options.BaseUrl =
                    "https://loyaltyhub.co.za";
            }

            _httpClient.Timeout =
                TimeSpan.FromSeconds(30);

            if (!_httpClient.DefaultRequestHeaders.Accept.Any())
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(
                        "application/json"));
            }
        }

        // =====================================================
        // SEARCH ALL SUPPORTED RETAILERS
        // =====================================================

        public async Task<
            Dictionary<string, RetailerProductResult?>>
            SearchProductAcrossRetailersAsync(
                string productName,
                CancellationToken cancellationToken = default)
        {
            Dictionary<string, RetailerProductResult?> results =
                SupportedRetailers.ToDictionary(
                    retailer => retailer,
                    retailer => (RetailerProductResult?)null,
                    StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(productName))
            {
                return results;
            }

            string originalSearchTerm =
                productName.Trim();

            // =================================================
            // FIRST SEARCH
            // =================================================

            GroceryPriceApiResponse? response =
                await SearchApiAsync(
                    originalSearchTerm,
                    originalSearchTerm,
                    cancellationToken);

            // =================================================
            // FALLBACK SEARCH
            // =================================================
            //
            // Example:
            //
            // Coca-Cola 2L
            //
            // may return no rows while:
            //
            // Coca-Cola
            //
            // returns many products.
            //
            // Matching still uses "Coca-Cola 2L".
            // =================================================

            if (response == null ||
                response.Data == null ||
                response.Data.Count == 0)
            {
                string fallbackSearchTerm =
                    BuildFallbackSearchTerm(
                        originalSearchTerm);

                if (!string.IsNullOrWhiteSpace(
                        fallbackSearchTerm) &&
                    !fallbackSearchTerm.Equals(
                        originalSearchTerm,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "No rows were returned for {OriginalProduct}. Trying broader search {FallbackProduct}.",
                        originalSearchTerm,
                        fallbackSearchTerm);

                    response =
                        await SearchApiAsync(
                            fallbackSearchTerm,
                            originalSearchTerm,
                            cancellationToken);
                }
            }

            if (response == null ||
                response.Data == null ||
                response.Data.Count == 0)
            {
                _logger.LogInformation(
                    "The grocery-price API returned no products for {Product}.",
                    originalSearchTerm);

                return results;
            }

            // =================================================
            // FIND BEST RESULT FOR EACH RETAILER
            // =================================================

            foreach (string retailer in SupportedRetailers)
            {
                GroceryPriceApiProduct? bestProduct =
                    FindBestProduct(
                        response.Data,
                        retailer,
                        originalSearchTerm);

                if (bestProduct == null)
                {
                    _logger.LogInformation(
                        "No reliable {Retailer} match was found for {Product}.",
                        retailer,
                        originalSearchTerm);

                    continue;
                }

                RetailerProductResult? mapped =
                    MapProduct(
                        bestProduct,
                        retailer,
                        originalSearchTerm);

                if (mapped != null)
                {
                    results[retailer] =
                        mapped;

                    _logger.LogInformation(
                        "Reliable {Retailer} match found for {Product}: {MatchedProduct} at R{Price}.",
                        retailer,
                        originalSearchTerm,
                        mapped.ProductName,
                        mapped.Price);
                }
            }

            return results;
        }

        // =====================================================
        // SEARCH ONE RETAILER
        // =====================================================

        public async Task<RetailerProductResult?>
            SearchProductAsync(
                string productName,
                string retailerName,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(productName) ||
                string.IsNullOrWhiteSpace(retailerName))
            {
                return null;
            }

            string? supportedRetailer =
                SupportedRetailers.FirstOrDefault(
                    retailer =>
                        retailer.Equals(
                            retailerName.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            if (supportedRetailer == null)
            {
                _logger.LogWarning(
                    "Retailer {Retailer} is not currently supported by the grocery-price service.",
                    retailerName);

                return null;
            }

            Dictionary<string, RetailerProductResult?> results =
                await SearchProductAcrossRetailersAsync(
                    productName,
                    cancellationToken);

            return results.TryGetValue(
                    supportedRetailer,
                    out RetailerProductResult? result)
                ? result
                : null;
        }

        // =====================================================
        // SEARCH EXTERNAL API WITH PAGINATION
        // =====================================================

        private async Task<GroceryPriceApiResponse?>
            SearchApiAsync(
                string searchTerm,
                string matchingTerm,
                CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogError(
                    "LoyaltyHub API key is missing. Add LoyaltyHub:ApiKey to ASP.NET Core User Secrets.");

                return null;
            }

            string baseUrl =
                _options.BaseUrl.TrimEnd('/');

            string encodedSearch =
                Uri.EscapeDataString(
                    searchTerm);

            List<GroceryPriceApiProduct> allProducts =
                new();

            HashSet<int> requestedOffsets =
                new();

            int currentOffset = 0;
            int pageNumber = 1;

            GroceryPriceApiMeta? latestMeta =
                null;

            _logger.LogInformation(
                "Searching grocery-price API for {Product}.",
                searchTerm);

            // =================================================
            // PAGINATION LOOP
            // =================================================

            while (pageNumber <= MaximumPagesPerSearch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!requestedOffsets.Add(currentOffset))
                {
                    _logger.LogWarning(
                        "Pagination stopped for {Product} because offset {Offset} was already requested.",
                        searchTerm,
                        currentOffset);

                    break;
                }

                string requestUrl;

                if (currentOffset <= 0)
                {
                    requestUrl =
                        $"{baseUrl}/api/v1/prices?search={encodedSearch}";
                }
                else
                {
                    requestUrl =
                        $"{baseUrl}/api/v1/prices?search={encodedSearch}&offset={currentOffset}";
                }

                _logger.LogInformation(
                    "Requesting grocery-price page {Page} for {Product} at offset {Offset}.",
                    pageNumber,
                    searchTerm,
                    currentOffset);

                GroceryPriceApiResponse? pageResponse =
                    await SendSearchRequestAsync(
                        requestUrl,
                        searchTerm,
                        pageNumber,
                        currentOffset,
                        cancellationToken);

                if (pageResponse == null)
                {
                    if (pageNumber == 1 &&
                        allProducts.Count == 0)
                    {
                        return null;
                    }

                    _logger.LogWarning(
                        "Pagination stopped for {Product} because page {Page} could not be retrieved.",
                        searchTerm,
                        pageNumber);

                    break;
                }

                pageResponse.Data ??=
                    new List<GroceryPriceApiProduct>();

                latestMeta =
                    pageResponse.Meta;

                foreach (GroceryPriceApiProduct product
                         in pageResponse.Data)
                {
                    if (!ContainsSameProduct(
                            allProducts,
                            product))
                    {
                        allProducts.Add(
                            product);
                    }
                }

                _logger.LogInformation(
                    "Grocery-price search for {Product} now has {Count} unique product rows after page {Page}.",
                    searchTerm,
                    allProducts.Count,
                    pageNumber);

                // =================================================
                // EARLY STOP
                // =================================================
                //
                // Stop only when the improved matcher can find
                // reliable products for all three retailers.
                // =================================================

                if (HasReliableMatchForAllRetailers(
                        allProducts,
                        matchingTerm))
                {
                    _logger.LogInformation(
                        "Pagination stopped early for {Product} because reliable matches were found for all supported retailers.",
                        matchingTerm);

                    break;
                }

                if (pageResponse.Meta == null)
                {
                    _logger.LogInformation(
                        "Pagination stopped for {Product} because the API returned no pagination metadata.",
                        searchTerm);

                    break;
                }

                bool hasMore =
                    pageResponse.Meta.HasMore == true;

                if (!hasMore)
                {
                    _logger.LogInformation(
                        "Pagination completed for {Product}. The API reports no more pages.",
                        searchTerm);

                    break;
                }

                int? nextOffset =
                    pageResponse.Meta.NextOffset;

                if (!nextOffset.HasValue)
                {
                    _logger.LogWarning(
                        "Pagination stopped for {Product}: HasMore=True but NextOffset was not supplied.",
                        searchTerm);

                    break;
                }

                if (nextOffset.Value <= currentOffset)
                {
                    _logger.LogWarning(
                        "Pagination stopped for {Product} because NextOffset {NextOffset} did not advance beyond current offset {CurrentOffset}.",
                        searchTerm,
                        nextOffset.Value,
                        currentOffset);

                    break;
                }

                currentOffset =
                    nextOffset.Value;

                pageNumber++;
            }

            if (pageNumber > MaximumPagesPerSearch)
            {
                _logger.LogWarning(
                    "Pagination safety limit of {MaximumPages} pages was reached for {Product}.",
                    MaximumPagesPerSearch,
                    searchTerm);
            }

            _logger.LogInformation(
                "Grocery-price API search completed for {Product} with {Count} unique product rows.",
                searchTerm,
                allProducts.Count);

            return new GroceryPriceApiResponse
            {
                Data =
                    allProducts,

                Meta =
                    latestMeta
            };
        }

        // =====================================================
        // SEND ONE API PAGE
        // =====================================================

        private async Task<GroceryPriceApiResponse?>
            SendSearchRequestAsync(
                string requestUrl,
                string searchTerm,
                int pageNumber,
                int offset,
                CancellationToken cancellationToken)
        {
            try
            {
                using HttpRequestMessage request =
                    new(
                        HttpMethod.Get,
                        requestUrl);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        _options.ApiKey);

                request.Headers.TryAddWithoutValidation(
                    "x-api-key",
                    _options.ApiKey);

                using HttpResponseMessage response =
                    await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseContentRead,
                        cancellationToken);

                _logger.LogInformation(
                    "Grocery-price API returned HTTP {StatusCode} for {Product}, page {Page}, offset {Offset}.",
                    (int)response.StatusCode,
                    searchTerm,
                    pageNumber,
                    offset);

                string json =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Grocery-price API request failed for {Product}, page {Page}. HTTP {StatusCode}. Response: {Response}",
                        searchTerm,
                        pageNumber,
                        (int)response.StatusCode,
                        LimitLogText(
                            json,
                            2000));

                    return null;
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning(
                        "Grocery-price API returned an empty response for {Product}, page {Page}.",
                        searchTerm,
                        pageNumber);

                    return null;
                }

                GroceryPriceApiResponse? apiResponse;

                try
                {
                    apiResponse =
                        JsonSerializer.Deserialize<
                            GroceryPriceApiResponse>(
                            json,
                            JsonOptions);
                }
                catch (JsonException exception)
                {
                    _logger.LogError(
                        exception,
                        "Could not deserialize grocery-price API response for {Product}, page {Page}. Response: {Response}",
                        searchTerm,
                        pageNumber,
                        LimitLogText(
                            json,
                            3000));

                    return null;
                }

                if (apiResponse == null)
                {
                    _logger.LogWarning(
                        "Grocery-price API response deserialized to null for {Product}, page {Page}.",
                        searchTerm,
                        pageNumber);

                    return null;
                }

                apiResponse.Data ??=
                    new List<GroceryPriceApiProduct>();

                _logger.LogInformation(
                    "Grocery-price API returned {Count} product rows for {Product}, page {Page}.",
                    apiResponse.Data.Count,
                    searchTerm,
                    pageNumber);

                if (apiResponse.Meta != null)
                {
                    _logger.LogInformation(
                        "Grocery-price pagination for {Product}: Page={Page}, Count={Count}, Total={Total}, Limit={Limit}, Offset={Offset}, HasMore={HasMore}, NextOffset={NextOffset}.",
                        searchTerm,
                        pageNumber,
                        apiResponse.Meta.Count,
                        apiResponse.Meta.Total,
                        apiResponse.Meta.Limit,
                        apiResponse.Meta.Offset,
                        apiResponse.Meta.HasMore,
                        apiResponse.Meta.NextOffset);
                }
                else
                {
                    _logger.LogInformation(
                        "Grocery-price API returned no pagination metadata for {Product}, page {Page}.",
                        searchTerm,
                        pageNumber);
                }

                return apiResponse;
            }
            catch (TaskCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    exception,
                    "Grocery-price API request timed out for {Product}, page {Page}.",
                    searchTerm,
                    pageNumber);

                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(
                    exception,
                    "HTTP error while searching grocery-price API for {Product}, page {Page}.",
                    searchTerm,
                    pageNumber);

                return null;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected error while searching grocery-price API for {Product}, page {Page}.",
                    searchTerm,
                    pageNumber);

                return null;
            }
        }

        // =====================================================
        // DO WE ALREADY HAVE ALL THREE RETAILERS?
        // =====================================================

        private bool HasReliableMatchForAllRetailers(
            IEnumerable<GroceryPriceApiProduct> products,
            string matchingTerm)
        {
            foreach (string retailer in SupportedRetailers)
            {
                GroceryPriceApiProduct? match =
                    FindBestProduct(
                        products,
                        retailer,
                        matchingTerm);

                if (match == null)
                {
                    return false;
                }
            }

            return true;
        }

        // =====================================================
        // DUPLICATE PRODUCT PROTECTION
        // =====================================================

        private static bool ContainsSameProduct(
            IEnumerable<GroceryPriceApiProduct> existingProducts,
            GroceryPriceApiProduct candidate)
        {
            return existingProducts.Any(
                existing =>
                {
                    if (!string.IsNullOrWhiteSpace(
                            existing.Id) &&
                        !string.IsNullOrWhiteSpace(
                            candidate.Id))
                    {
                        return existing.Id.Equals(
                            candidate.Id,
                            StringComparison.OrdinalIgnoreCase);
                    }

                    string existingBarcode =
                        !string.IsNullOrWhiteSpace(
                            existing.Barcode)
                            ? existing.Barcode
                            : existing.Gtin ?? string.Empty;

                    string candidateBarcode =
                        !string.IsNullOrWhiteSpace(
                            candidate.Barcode)
                            ? candidate.Barcode
                            : candidate.Gtin ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(
                            existingBarcode) &&
                        !string.IsNullOrWhiteSpace(
                            candidateBarcode) &&
                        existingBarcode.Equals(
                            candidateBarcode,
                            StringComparison.OrdinalIgnoreCase) &&
                        NormalizeRetailerName(
                            GetRetailerName(existing))
                            .Equals(
                                NormalizeRetailerName(
                                    GetRetailerName(candidate)),
                                StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (!string.IsNullOrWhiteSpace(
                            existing.RetailerSku) &&
                        !string.IsNullOrWhiteSpace(
                            candidate.RetailerSku))
                    {
                        return existing.RetailerSku.Equals(
                                   candidate.RetailerSku,
                                   StringComparison.OrdinalIgnoreCase) &&
                               NormalizeRetailerName(
                                   GetRetailerName(existing))
                                   .Equals(
                                       NormalizeRetailerName(
                                           GetRetailerName(candidate)),
                                       StringComparison.OrdinalIgnoreCase);
                    }

                    return false;
                });
        }

        // =====================================================
        // FIND BEST PRODUCT
        // =====================================================

        private GroceryPriceApiProduct?
            FindBestProduct(
                IEnumerable<GroceryPriceApiProduct> products,
                string retailer,
                string searchTerm)
        {
            List<GroceryPriceApiProduct> candidates =
                products
                    .Where(
                        product =>
                            ProductBelongsToRetailer(
                                product,
                                retailer))
                    .Where(
                        product =>
                            GetEffectivePrice(product) > 0)
                    .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            string normalizedSearch =
                NormalizeText(
                    searchTerm);

            string? requestedSize =
                ExtractSize(
                    normalizedSearch);

            HashSet<string> requestedVariants =
                ExtractVariantTerms(
                    normalizedSearch);

            bool requestHasExplicitVariant =
                requestedVariants.Count > 0;

            GroceryPriceApiProduct? bestProduct =
                candidates
                    .Select(
                        product =>
                        {
                            string productName =
                                GetProductName(
                                    product);

                            string normalizedProduct =
                                NormalizeText(
                                    productName);

                            double score =
                                CalculateMatchScore(
                                    searchTerm,
                                    productName);

                            string? apiSize =
                                GetNormalizedApiSize(
                                    product);

                            HashSet<string> productVariants =
                                ExtractVariantTerms(
                                    normalizedProduct);

                            bool productHasAlternativeVariant =
                                HasAlternativeVariant(
                                    normalizedProduct);

                            // =================================
                            // STRICT SIZE CHECK
                            // =================================

                            if (!string.IsNullOrWhiteSpace(
                                    requestedSize) &&
                                !string.IsNullOrWhiteSpace(
                                    apiSize))
                            {
                                if (requestedSize.Equals(
                                        apiSize,
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    score += 0.25;
                                }
                                else
                                {
                                    // Wrong structured size is a
                                    // major mismatch.
                                    score -= 0.65;
                                }
                            }

                            // =================================
                            // VARIANT MATCHING
                            // =================================

                            if (requestHasExplicitVariant)
                            {
                                int matchedVariants =
                                    requestedVariants.Count(
                                        variant =>
                                            productVariants.Contains(
                                                variant));

                                if (matchedVariants ==
                                    requestedVariants.Count)
                                {
                                    // Exact requested variant.
                                    score += 0.30;
                                }
                                else
                                {
                                    // User explicitly requested
                                    // something like Zero Sugar,
                                    // Diet, Decaf, Low Fat, etc.
                                    //
                                    // Do not silently substitute
                                    // another variant.
                                    score -= 0.65;
                                }

                                // Product contains a conflicting
                                // variant not present in the
                                // requested variant set.
                                if (HasConflictingVariant(
                                        requestedVariants,
                                        productVariants))
                                {
                                    score -= 0.45;
                                }
                            }
                            else
                            {
                                // =================================
                                // GENERIC PRODUCT REQUEST
                                // =================================
                                //
                                // Example:
                                //
                                // Coca-Cola 2L
                                //
                                // The user did not request Zero,
                                // Diet, Light, Decaf, etc.
                                //
                                // Prefer the standard/original
                                // product rather than automatically
                                // selecting a special variant.
                                // =================================

                                if (productHasAlternativeVariant)
                                {
                                    score -= 0.45;
                                }

                                if (IsOriginalVariant(
                                        normalizedProduct))
                                {
                                    score += 0.20;
                                }
                            }

                            return new
                            {
                                Product =
                                    product,

                                Score =
                                    Math.Clamp(
                                        score,
                                        0,
                                        1),

                                SizeMatches =
                                    SizeMatches(
                                        requestedSize,
                                        apiSize),

                                VariantCompatible =
                                    IsVariantCompatible(
                                        requestedVariants,
                                        normalizedProduct)
                            };
                        })
                    // =========================================
                    // SIZE PROTECTION
                    // =========================================
                    //
                    // If both sides have explicit structured
                    // sizes, wrong sizes are rejected.
                    // =========================================

                    .Where(
                        item =>
                            item.SizeMatches)

                    // =========================================
                    // VARIANT PROTECTION
                    // =========================================

                    .Where(
                        item =>
                            item.VariantCompatible)

                    // =========================================
                    // RELIABILITY THRESHOLD
                    // =========================================

                    .Where(
                        item =>
                            item.Score >= 0.55)

                    .OrderByDescending(
                        item =>
                            item.Score)

                    .ThenByDescending(
                        item =>
                            IsAvailable(
                                item.Product))

                    .ThenByDescending(
                        item =>
                            GetLastUpdated(
                                item.Product))

                    .Select(
                        item =>
                            item.Product)

                    .FirstOrDefault();

            return bestProduct;
        }

        // =====================================================
        // SIZE COMPATIBILITY
        // =====================================================

        private static bool SizeMatches(
            string? requestedSize,
            string? productSize)
        {
            if (string.IsNullOrWhiteSpace(
                    requestedSize))
            {
                return true;
            }

            // We cannot reject the product solely because the
            // provider omitted structured size information.
            //
            // CalculateMatchScore still checks size appearing
            // inside the product name.
            if (string.IsNullOrWhiteSpace(
                    productSize))
            {
                return true;
            }

            return requestedSize.Equals(
                productSize,
                StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================
        // EXTRACT VARIANT TERMS
        // =====================================================

        private static HashSet<string> ExtractVariantTerms(
            string normalizedText)
        {
            HashSet<string> variants =
                new(
                    StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(
                    normalizedText))
            {
                return variants;
            }

            string paddedText =
                " " +
                normalizedText +
                " ";

            foreach (string phrase in VariantPhrases)
            {
                string normalizedPhrase =
                    NormalizeText(
                        phrase);

                string paddedPhrase =
                    " " +
                    normalizedPhrase +
                    " ";

                if (paddedText.Contains(
                        paddedPhrase,
                        StringComparison.OrdinalIgnoreCase))
                {
                    variants.Add(
                        normalizedPhrase);
                }
            }

            return variants;
        }

        // =====================================================
        // ALTERNATIVE VARIANT CHECK
        // =====================================================

        private static bool HasAlternativeVariant(
            string normalizedProductName)
        {
            if (string.IsNullOrWhiteSpace(
                    normalizedProductName))
            {
                return false;
            }

            HashSet<string> variants =
                ExtractVariantTerms(
                    normalizedProductName);

            foreach (string variant in variants)
            {
                // Original is not considered an alternative
                // variant for a generic product request.
                if (variant.Equals(
                        "original",
                        StringComparison.OrdinalIgnoreCase) ||
                    variant.Equals(
                        "original taste",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return true;
            }

            foreach (string term in AlternativeVariantTerms)
            {
                if (ContainsWholePhrase(
                        normalizedProductName,
                        NormalizeText(term)))
                {
                    return true;
                }
            }

            return false;
        }

        // =====================================================
        // ORIGINAL / STANDARD PRODUCT CHECK
        // =====================================================

        private static bool IsOriginalVariant(
            string normalizedProductName)
        {
            if (string.IsNullOrWhiteSpace(
                    normalizedProductName))
            {
                return false;
            }

            return
                ContainsWholePhrase(
                    normalizedProductName,
                    "original") ||
                ContainsWholePhrase(
                    normalizedProductName,
                    "original taste");
        }

        // =====================================================
        // VARIANT COMPATIBILITY
        // =====================================================

        private static bool IsVariantCompatible(
            HashSet<string> requestedVariants,
            string normalizedProductName)
        {
            HashSet<string> productVariants =
                ExtractVariantTerms(
                    normalizedProductName);

            // ---------------------------------------------
            // User explicitly requested a variant.
            // ---------------------------------------------

            if (requestedVariants.Count > 0)
            {
                foreach (string requestedVariant
                         in requestedVariants)
                {
                    if (!productVariants.Contains(
                            requestedVariant))
                    {
                        return false;
                    }
                }

                if (HasConflictingVariant(
                        requestedVariants,
                        productVariants))
                {
                    return false;
                }

                return true;
            }

            // ---------------------------------------------
            // Generic request.
            // ---------------------------------------------
            //
            // Coca-Cola 2L should not automatically become
            // Coca-Cola Zero Sugar 2L.
            // ---------------------------------------------

            if (HasAlternativeVariant(
                    normalizedProductName))
            {
                return false;
            }

            return true;
        }

        // =====================================================
        // CONFLICTING VARIANT CHECK
        // =====================================================

        private static bool HasConflictingVariant(
            HashSet<string> requestedVariants,
            HashSet<string> productVariants)
        {
            if (requestedVariants.Count == 0 ||
                productVariants.Count == 0)
            {
                return false;
            }

            bool requestZeroSugar =
                requestedVariants.Contains(
                    "zero sugar") ||
                requestedVariants.Contains(
                    "no sugar") ||
                requestedVariants.Contains(
                    "sugar free");

            bool productZeroSugar =
                productVariants.Contains(
                    "zero sugar") ||
                productVariants.Contains(
                    "no sugar") ||
                productVariants.Contains(
                    "sugar free");

            bool requestZeroCaffeine =
                requestedVariants.Contains(
                    "zero caffeine") ||
                requestedVariants.Contains(
                    "caffeine free") ||
                requestedVariants.Contains(
                    "decaf") ||
                requestedVariants.Contains(
                    "decaffeinated");

            bool productZeroCaffeine =
                productVariants.Contains(
                    "zero caffeine") ||
                productVariants.Contains(
                    "caffeine free") ||
                productVariants.Contains(
                    "decaf") ||
                productVariants.Contains(
                    "decaffeinated");

            bool requestDiet =
                requestedVariants.Contains(
                    "diet");

            bool productDiet =
                productVariants.Contains(
                    "diet");

            bool requestLight =
                requestedVariants.Contains(
                    "light");

            bool productLight =
                productVariants.Contains(
                    "light");

            bool requestOriginal =
                requestedVariants.Contains(
                    "original") ||
                requestedVariants.Contains(
                    "original taste");

            bool productOriginal =
                productVariants.Contains(
                    "original") ||
                productVariants.Contains(
                    "original taste");

            // ---------------------------------------------
            // Explicit Original should not become Zero/Diet.
            // ---------------------------------------------

            if (requestOriginal &&
                (productZeroSugar ||
                 productDiet ||
                 productLight ||
                 productZeroCaffeine))
            {
                return true;
            }

            // ---------------------------------------------
            // Explicit Zero/Diet/etc should not become
            // Original.
            // ---------------------------------------------

            if ((requestZeroSugar ||
                 requestDiet ||
                 requestLight ||
                 requestZeroCaffeine) &&
                productOriginal)
            {
                return true;
            }

            // ---------------------------------------------
            // User requested Zero Caffeine.
            // Product must also be Zero Caffeine.
            // ---------------------------------------------

            if (requestZeroCaffeine &&
                !productZeroCaffeine)
            {
                return true;
            }

            // ---------------------------------------------
            // User requested Diet specifically.
            // ---------------------------------------------

            if (requestDiet &&
                !productDiet)
            {
                return true;
            }

            // ---------------------------------------------
            // User requested Light specifically.
            // ---------------------------------------------

            if (requestLight &&
                !productLight)
            {
                return true;
            }

            // ---------------------------------------------
            // User requested Zero Sugar specifically.
            // ---------------------------------------------

            if (requestZeroSugar &&
                !productZeroSugar)
            {
                return true;
            }

            return false;
        }

        // =====================================================
        // WHOLE PHRASE CHECK
        // =====================================================

        private static bool ContainsWholePhrase(
            string normalizedText,
            string normalizedPhrase)
        {
            if (string.IsNullOrWhiteSpace(
                    normalizedText) ||
                string.IsNullOrWhiteSpace(
                    normalizedPhrase))
            {
                return false;
            }

            string text =
                " " +
                normalizedText.Trim() +
                " ";

            string phrase =
                " " +
                normalizedPhrase.Trim() +
                " ";

            return text.Contains(
                phrase,
                StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================
        // MAP PRODUCT
        // =====================================================

        private RetailerProductResult?
            MapProduct(
                GroceryPriceApiProduct product,
                string retailer,
                string searchTerm)
        {
            decimal? effectivePrice =
                GetEffectivePrice(
                    product);

            if (!effectivePrice.HasValue ||
                effectivePrice.Value <= 0)
            {
                return null;
            }

            string productName =
                GetProductName(
                    product);

            if (string.IsNullOrWhiteSpace(
                    productName))
            {
                return null;
            }

            decimal? regularPrice =
                product.RegularPrice;

            bool isPromotion =
                product.IsOnPromotion == true ||
                product.Promotion == true;

            if (product.SalePrice.HasValue &&
                product.SalePrice.Value > 0)
            {
                if (product.Price.HasValue &&
                    product.Price.Value >
                    product.SalePrice.Value)
                {
                    regularPrice =
                        product.Price;

                    isPromotion =
                        true;
                }
            }

            string? productUrl =
                !string.IsNullOrWhiteSpace(
                    product.ProductUrl)
                    ? product.ProductUrl
                    : product.Url;

            DateTime checkedAt =
                GetLastUpdated(
                    product);

            return new RetailerProductResult
            {
                RetailerName =
                    retailer,

                SearchTerm =
                    searchTerm,

                ProductName =
                    productName,

                Brand =
                    product.Brand,

                Size =
                    FormatProductSize(
                        product),

                Price =
                    effectivePrice.Value,

                RegularPrice =
                    regularPrice,

                IsOnPromotion =
                    isPromotion,

                IsAvailable =
                    IsAvailable(
                        product),

                ProductUrl =
                    productUrl,

                PriceCheckedAt =
                    checkedAt,

                PriceSource =
                    "LoyaltyHub Grocery Price API"
            };
        }

        // =====================================================
        // FORMAT PRODUCT SIZE
        // =====================================================

        private static string? FormatProductSize(
            GroceryPriceApiProduct product)
        {
            if (!string.IsNullOrWhiteSpace(
                    product.UnitSize))
            {
                return product.UnitSize.Trim();
            }

            if (product.Size == null ||
                !product.Size.Quantity.HasValue ||
                string.IsNullOrWhiteSpace(
                    product.Size.Unit))
            {
                return ExtractDisplaySizeFromName(
                    GetProductName(
                        product));
            }

            decimal quantity =
                product.Size.Quantity.Value;

            string unit =
                product.Size.Unit
                    .Trim()
                    .ToLowerInvariant();

            if (unit == "l")
            {
                if (quantity < 1)
                {
                    decimal millilitres =
                        quantity * 1000m;

                    return
                        $"{FormatNumber(millilitres)}ml";
                }

                return
                    $"{FormatNumber(quantity)}L";
            }

            if (unit == "kg")
            {
                if (quantity < 1)
                {
                    decimal grams =
                        quantity * 1000m;

                    return
                        $"{FormatNumber(grams)}g";
                }

                return
                    $"{FormatNumber(quantity)}kg";
            }

            return
                $"{FormatNumber(quantity)}{unit}";
        }

        // =====================================================
        // FORMAT NUMBER
        // =====================================================

        private static string FormatNumber(
            decimal value)
        {
            return value.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
        }

        // =====================================================
        // EXTRACT DISPLAY SIZE FROM NAME
        // =====================================================

        private static string?
            ExtractDisplaySizeFromName(
                string productName)
        {
            if (string.IsNullOrWhiteSpace(
                    productName))
            {
                return null;
            }

            Match match =
                Regex.Match(
                    productName,
                    @"(?<number>\d+(?:[.,]\d+)?)\s*(?<unit>kg|g|mg|l|ml|cl)\b",
                    RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return null;
            }

            string number =
                match.Groups["number"]
                    .Value
                    .Replace(
                        ",",
                        ".");

            string unit =
                match.Groups["unit"]
                    .Value
                    .ToLowerInvariant();

            if (unit == "l")
            {
                unit = "L";
            }

            return number + unit;
        }

        // =====================================================
        // NORMALIZED STRUCTURED API SIZE
        // =====================================================

        private static string?
            GetNormalizedApiSize(
                GroceryPriceApiProduct product)
        {
            string? formatted =
                FormatProductSize(
                    product);

            if (string.IsNullOrWhiteSpace(
                    formatted))
            {
                return null;
            }

            return ExtractSize(
                NormalizeText(
                    formatted));
        }

        // =====================================================
        // BUILD FALLBACK SEARCH TERM
        // =====================================================

        private static string BuildFallbackSearchTerm(
            string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(
                    searchTerm))
            {
                return string.Empty;
            }

            string fallback =
                Regex.Replace(
                    searchTerm,
                    @"\b\d+(?:[.,]\d+)?\s*(?:kg|g|mg|l|ml|cl)\b",
                    " ",
                    RegexOptions.IgnoreCase);

            fallback =
                Regex.Replace(
                    fallback,
                    @"\s+",
                    " ");

            return fallback.Trim();
        }

        // =====================================================
        // PRODUCT NAME
        // =====================================================

        private static string GetProductName(
            GroceryPriceApiProduct product)
        {
            if (!string.IsNullOrWhiteSpace(
                    product.ProductName))
            {
                return product.ProductName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(
                    product.Name))
            {
                return product.Name.Trim();
            }

            return string.Empty;
        }

        // =====================================================
        // RETAILER NAME
        // =====================================================

        private static string GetRetailerName(
            GroceryPriceApiProduct product)
        {
            if (!string.IsNullOrWhiteSpace(
                    product.RetailerName))
            {
                return product.RetailerName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(
                    product.Retailer))
            {
                return product.Retailer.Trim();
            }

            return string.Empty;
        }

        // =====================================================
        // RETAILER MATCH
        // =====================================================

        private static bool ProductBelongsToRetailer(
            GroceryPriceApiProduct product,
            string retailer)
        {
            string sourceRetailer =
                NormalizeRetailerName(
                    GetRetailerName(
                        product));

            string requestedRetailer =
                NormalizeRetailerName(
                    retailer);

            if (string.IsNullOrWhiteSpace(
                    sourceRetailer))
            {
                return false;
            }

            return sourceRetailer.Equals(
                requestedRetailer,
                StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================
        // NORMALIZE RETAILER
        // =====================================================

        private static string NormalizeRetailerName(
            string retailer)
        {
            if (string.IsNullOrWhiteSpace(
                    retailer))
            {
                return string.Empty;
            }

            string normalized =
                retailer
                    .Trim()
                    .ToLowerInvariant();

            normalized =
                normalized.Replace(
                    "&",
                    "and");

            normalized =
                string.Join(
                    " ",
                    normalized.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries));

            if (normalized is
                "pnp" or
                "picknpay" or
                "pick n pay")
            {
                return "pick n pay";
            }

            if (normalized.Contains(
                    "checkers",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "checkers";
            }

            if (normalized.Contains(
                    "shoprite",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "shoprite";
            }

            return normalized;
        }

        // =====================================================
        // EFFECTIVE PRICE
        // =====================================================

        private static decimal? GetEffectivePrice(
            GroceryPriceApiProduct product)
        {
            if (product.SalePrice.HasValue &&
                product.SalePrice.Value > 0)
            {
                return product.SalePrice.Value;
            }

            if (product.Price.HasValue &&
                product.Price.Value > 0)
            {
                return product.Price.Value;
            }

            return null;
        }

        // =====================================================
        // AVAILABILITY
        // =====================================================

        private static bool IsAvailable(
            GroceryPriceApiProduct product)
        {
            if (product.IsAvailable.HasValue)
            {
                return product.IsAvailable.Value;
            }

            if (product.InStock.HasValue)
            {
                return product.InStock.Value;
            }

            // Provider did not confirm stock status.
            return true;
        }

        // =====================================================
        // LAST UPDATED
        // =====================================================

        private static DateTime GetLastUpdated(
            GroceryPriceApiProduct product)
        {
            DateTime? timestamp =
                product.LastSeenAt ??
                product.PriceChangedAt ??
                product.PriceUpdatedAt ??
                product.UpdatedAt;

            if (!timestamp.HasValue)
            {
                return DateTime.UtcNow;
            }

            DateTime value =
                timestamp.Value;

            if (value.Kind ==
                DateTimeKind.Utc)
            {
                return value;
            }

            if (value.Kind ==
                DateTimeKind.Local)
            {
                return value.ToUniversalTime();
            }

            return DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc);
        }

        // =====================================================
        // PRODUCT MATCHING
        // =====================================================

        private static double CalculateMatchScore(
            string searchTerm,
            string productName)
        {
            if (string.IsNullOrWhiteSpace(
                    searchTerm) ||
                string.IsNullOrWhiteSpace(
                    productName))
            {
                return 0;
            }

            string normalizedSearch =
                NormalizeText(
                    searchTerm);

            string normalizedProduct =
                NormalizeText(
                    productName);

            if (string.IsNullOrWhiteSpace(
                    normalizedSearch) ||
                string.IsNullOrWhiteSpace(
                    normalizedProduct))
            {
                return 0;
            }

            HashSet<string> searchWords =
                normalizedSearch
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries)
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            HashSet<string> productWords =
                normalizedProduct
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries)
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            if (searchWords.Count == 0 ||
                productWords.Count == 0)
            {
                return 0;
            }

            int matchedWords =
                searchWords.Count(
                    word =>
                        productWords.Contains(
                            word));

            double score =
                (double)matchedWords /
                searchWords.Count;

            if (normalizedProduct.Contains(
                    normalizedSearch,
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 0.25;
            }

            string? requestedSize =
                ExtractSize(
                    normalizedSearch);

            string? productSize =
                ExtractSize(
                    normalizedProduct);

            if (!string.IsNullOrWhiteSpace(
                    requestedSize))
            {
                if (string.Equals(
                        requestedSize,
                        productSize,
                        StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.20;
                }
                else if (!string.IsNullOrWhiteSpace(
                             productSize))
                {
                    score -= 0.30;
                }
            }

            return Math.Clamp(
                score,
                0,
                1);
        }

        // =====================================================
        // NORMALIZE PRODUCT TEXT
        // =====================================================

        private static string NormalizeText(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return string.Empty;
            }

            string normalized =
                value
                    .ToLowerInvariant()
                    .Trim();

            normalized =
                Regex.Replace(
                    normalized,
                    @"(\d)\s+(kg|g|mg|l|ml|cl)\b",
                    "$1$2",
                    RegexOptions.IgnoreCase);

            normalized =
                Regex.Replace(
                    normalized,
                    @"[^a-z0-9]+",
                    " ");

            normalized =
                Regex.Replace(
                    normalized,
                    @"\s+",
                    " ");

            return normalized.Trim();
        }

        // =====================================================
        // EXTRACT SIZE
        // =====================================================

        private static string? ExtractSize(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }

            Match match =
                Regex.Match(
                    value,
                    @"(?<size>\d+(?:[.,]\d+)?\s*(?:kg|g|mg|l|ml|cl))\b",
                    RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return null;
            }

            return NormalizeText(
                match.Groups["size"].Value);
        }

        // =====================================================
        // LOG SAFETY
        // =====================================================

        private static string LimitLogText(
            string? value,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return string.Empty;
            }

            string cleaned =
                value
                    .Replace(
                        "\r",
                        " ")
                    .Replace(
                        "\n",
                        " ")
                    .Trim();

            if (cleaned.Length <=
                maximumLength)
            {
                return cleaned;
            }

            return cleaned.Substring(
                       0,
                       maximumLength)
                   + "...";
        }
    }
}