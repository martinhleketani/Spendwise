using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using SpendWise.API.Models;

namespace SpendWise.API.Services.Retailers
{
    // =========================================================
    // PICK N PAY PRICE PROVIDER
    // =========================================================
    //
    // Retrieves publicly available product information from
    // Pick n Pay's online shopping website.
    //
    // IMPORTANT:
    //
    // SpendWise NEVER generates, estimates or guesses prices.
    //
    // A price is returned only after the provider opens a
    // Pick n Pay product page and verifies a product name and
    // a positive Rand price on that page.
    //
    // Pick n Pay states that product availability and local
    // pricing can depend on the customer's delivery area.
    // =========================================================

    public class PickNPayPriceProvider : IRetailerPriceProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PickNPayPriceProvider> _logger;

        private const string BaseUrl = "https://www.pnp.co.za";

        public string RetailerName => "Pick n Pay";

        public PickNPayPriceProvider(
            HttpClient httpClient,
            ILogger<PickNPayPriceProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromSeconds(25);

            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "SpendWise/1.0");
            }

            if (!_httpClient.DefaultRequestHeaders.Accept.Any())
            {
                _httpClient.DefaultRequestHeaders.Accept.ParseAdd(
                    "text/html,application/xhtml+xml");
            }
        }

        // =====================================================
        // SEARCH PRODUCT
        // =====================================================

        public async Task<RetailerProductResult?> SearchProductAsync(
            string productName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return null;
            }

            string searchTerm = productName.Trim();

            try
            {
                _logger.LogInformation(
                    "Searching Pick n Pay for {Product}.",
                    searchTerm);

                List<string> productUrls =
                    await DiscoverProductUrlsAsync(
                        searchTerm,
                        cancellationToken);

                if (productUrls.Count == 0)
                {
                    _logger.LogWarning(
                        "Pick n Pay search returned no product URLs for {Product}.",
                        searchTerm);

                    return null;
                }

                _logger.LogInformation(
                    "Pick n Pay returned {Count} candidate product URLs for {Product}.",
                    productUrls.Count,
                    searchTerm);

                List<ProductCandidate> verifiedCandidates =
                    new List<ProductCandidate>();

                // Do not hammer the retailer site.
                // Only verify the first few unique candidates.
                foreach (string productUrl in productUrls.Take(8))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ProductCandidate? candidate =
                        await VerifyProductPageAsync(
                            productUrl,
                            searchTerm,
                            cancellationToken);

                    if (candidate != null)
                    {
                        verifiedCandidates.Add(candidate);
                    }
                }

                if (verifiedCandidates.Count == 0)
                {
                    _logger.LogWarning(
                        "No verified Pick n Pay product page with a reliable price was found for {Product}.",
                        searchTerm);

                    return null;
                }

                ProductCandidate? best =
                    verifiedCandidates
                        .OrderByDescending(x => x.MatchScore)
                        .ThenByDescending(x => x.IsAvailable)
                        .FirstOrDefault();

                if (best == null ||
                    best.Price <= 0 ||
                    best.MatchScore < 0.50)
                {
                    _logger.LogWarning(
                        "Pick n Pay candidates for {Product} did not meet the reliability threshold.",
                        searchTerm);

                    return null;
                }

                _logger.LogInformation(
                    "Verified Pick n Pay product {MatchedProduct} at R{Price} for search {SearchTerm}.",
                    best.ProductName,
                    best.Price,
                    searchTerm);

                return new RetailerProductResult
                {
                    RetailerName = RetailerName,
                    SearchTerm = searchTerm,
                    ProductName = best.ProductName,
                    Brand = best.Brand,
                    Size = best.Size,
                    Price = best.Price,
                    RegularPrice = best.RegularPrice,
                    IsOnPromotion = best.IsOnPromotion,
                    IsAvailable = best.IsAvailable,
                    ProductUrl = best.ProductUrl,
                    PriceCheckedAt = DateTime.UtcNow,
                    PriceSource = "Pick n Pay Online"
                };
            }
            catch (TaskCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    exception,
                    "Pick n Pay request timed out for {Product}.",
                    searchTerm);

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
                    "HTTP error while retrieving Pick n Pay price for {Product}.",
                    searchTerm);

                return null;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected error while retrieving Pick n Pay price for {Product}.",
                    searchTerm);

                return null;
            }
        }

        // =====================================================
        // DISCOVER PRODUCT URLS
        // =====================================================

        private async Task<List<string>> DiscoverProductUrlsAsync(
            string searchTerm,
            CancellationToken cancellationToken)
        {
            string encoded =
                Uri.EscapeDataString(searchTerm);

            string searchUrl =
                $"{BaseUrl}/search/?text={encoded}";

            _logger.LogInformation(
                "Requesting Pick n Pay search URL {SearchUrl}.",
                searchUrl);

            using HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    searchUrl);

            using HttpResponseMessage response =
                await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken);

            _logger.LogInformation(
                "Pick n Pay search returned HTTP {StatusCode}.",
                (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                return new List<string>();
            }

            string html =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(html))
            {
                return new List<string>();
            }

            html = WebUtility.HtmlDecode(html);

            HashSet<string> urls =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            // -------------------------------------------------
            // Normal HTML links
            // -------------------------------------------------

            MatchCollection normalLinks =
                Regex.Matches(
                    html,
                    @"href\s*=\s*[""'](?<url>[^""']*?/p/[^""'#?]+(?:\?[^""']*)?)[""']",
                    RegexOptions.IgnoreCase);

            foreach (Match match in normalLinks)
            {
                AddProductUrl(
                    urls,
                    match.Groups["url"].Value);
            }

            // -------------------------------------------------
            // Escaped links inside script / JSON data
            //
            // Example:
            // \/product-name\/p\/000000...
            // -------------------------------------------------

            MatchCollection escapedLinks =
                Regex.Matches(
                    html,
                    @"(?<url>\\?/[^""'\s<>]*?\\?/p\\?/[^""'\s<>\\]+)",
                    RegexOptions.IgnoreCase);

            foreach (Match match in escapedLinks)
            {
                string value =
                    match.Groups["url"].Value
                        .Replace("\\/", "/")
                        .Replace("\\u002F", "/",
                            StringComparison.OrdinalIgnoreCase);

                AddProductUrl(
                    urls,
                    value);
            }

            // -------------------------------------------------
            // Absolute Pick n Pay product URLs
            // -------------------------------------------------

            MatchCollection absoluteLinks =
                Regex.Matches(
                    html,
                    @"https?://(?:www\.)?pnp\.co\.za/(?<path>[^""'\s<>]+?/p/[^""'\s<>]+)",
                    RegexOptions.IgnoreCase);

            foreach (Match match in absoluteLinks)
            {
                AddProductUrl(
                    urls,
                    match.Value);
            }

            return urls
                .Where(url =>
                    IsReasonableProductUrlForSearch(
                        url,
                        searchTerm))
                .Concat(
                    urls.Where(url =>
                        !IsReasonableProductUrlForSearch(
                            url,
                            searchTerm)))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();
        }

        // =====================================================
        // ADD PRODUCT URL
        // =====================================================

        private static void AddProductUrl(
            HashSet<string> urls,
            string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                return;
            }

            string url =
                WebUtility.HtmlDecode(rawUrl)
                    .Replace("\\/", "/")
                    .Trim();

            int fragmentIndex =
                url.IndexOf('#');

            if (fragmentIndex >= 0)
            {
                url =
                    url.Substring(
                        0,
                        fragmentIndex);
            }

            if (!url.Contains(
                    "/p/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (url.StartsWith(
                    "//",
                    StringComparison.Ordinal))
            {
                url =
                    "https:" + url;
            }
            else if (url.StartsWith(
                         "/",
                         StringComparison.Ordinal))
            {
                url =
                    BaseUrl + url;
            }
            else if (!url.StartsWith(
                         "http://",
                         StringComparison.OrdinalIgnoreCase) &&
                     !url.StartsWith(
                         "https://",
                         StringComparison.OrdinalIgnoreCase))
            {
                url =
                    BaseUrl + "/" + url.TrimStart('/');
            }

            if (!Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                return;
            }

            if (!uri.Host.EndsWith(
                    "pnp.co.za",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            urls.Add(
                uri.GetLeftPart(UriPartial.Path));
        }

        // =====================================================
        // VERIFY PRODUCT PAGE
        // =====================================================

        private async Task<ProductCandidate?> VerifyProductPageAsync(
            string productUrl,
            string searchTerm,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Verifying Pick n Pay product page {ProductUrl}.",
                productUrl);

            using HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    productUrl);

            using HttpResponseMessage response =
                await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Pick n Pay product page returned HTTP {StatusCode}: {ProductUrl}",
                    (int)response.StatusCode,
                    productUrl);

                return null;
            }

            string html =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            html =
                WebUtility.HtmlDecode(html);

            string? verifiedName =
                ExtractProductName(
                    html);

            if (string.IsNullOrWhiteSpace(
                    verifiedName))
            {
                verifiedName =
                    GetProductNameFromUrl(
                        productUrl);
            }

            if (string.IsNullOrWhiteSpace(
                    verifiedName))
            {
                return null;
            }

            double matchScore =
                CalculateMatchScore(
                    searchTerm,
                    verifiedName);

            if (matchScore < 0.50)
            {
                _logger.LogInformation(
                    "Rejected Pick n Pay candidate {Candidate}; match score {Score}.",
                    verifiedName,
                    matchScore);

                return null;
            }

            string visibleText =
                StripNonVisibleContent(
                    html);

            decimal? price =
                ExtractVerifiedPrice(
                    html,
                    visibleText,
                    verifiedName);

            if (!price.HasValue ||
                price.Value <= 0)
            {
                _logger.LogInformation(
                    "No verified price found on Pick n Pay product page {ProductUrl}.",
                    productUrl);

                return null;
            }

            bool unavailable =
                ContainsUnavailableText(
                    visibleText);

            bool promotion =
                ContainsPromotionText(
                    visibleText);

            decimal? regularPrice =
                promotion
                    ? ExtractRegularPrice(
                        visibleText,
                        price.Value)
                    : null;

            string? brand =
                ExtractBrand(
                    html,
                    visibleText,
                    verifiedName);

            string? size =
                ExtractSize(
                    verifiedName);

            return new ProductCandidate
            {
                ProductName = verifiedName,
                Brand = brand,
                Size = size,
                Price = price.Value,
                RegularPrice = regularPrice,
                IsOnPromotion = promotion,
                IsAvailable = !unavailable,
                ProductUrl = productUrl,
                MatchScore = matchScore
            };
        }

        // =====================================================
        // PRODUCT NAME
        // =====================================================

        private static string? ExtractProductName(
            string html)
        {
            // Try H1 first.
            Match h1 =
                Regex.Match(
                    html,
                    @"<h1\b[^>]*>(?<name>.*?)</h1>",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline);

            if (h1.Success)
            {
                string value =
                    StripHtml(
                        h1.Groups["name"].Value);

                if (IsValidProductName(value))
                {
                    return value;
                }
            }

            // OpenGraph title.
            Match ogTitle =
                Regex.Match(
                    html,
                    @"<meta\b[^>]*property\s*=\s*[""']og:title[""'][^>]*content\s*=\s*[""'](?<name>[^""']+)[""'][^>]*>",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline);

            if (!ogTitle.Success)
            {
                ogTitle =
                    Regex.Match(
                        html,
                        @"<meta\b[^>]*content\s*=\s*[""'](?<name>[^""']+)[""'][^>]*property\s*=\s*[""']og:title[""'][^>]*>",
                        RegexOptions.IgnoreCase |
                        RegexOptions.Singleline);
            }

            if (ogTitle.Success)
            {
                string value =
                    CleanProductTitle(
                        ogTitle.Groups["name"].Value);

                if (IsValidProductName(value))
                {
                    return value;
                }
            }

            // Standard title fallback.
            Match title =
                Regex.Match(
                    html,
                    @"<title\b[^>]*>(?<name>.*?)</title>",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline);

            if (title.Success)
            {
                string value =
                    CleanProductTitle(
                        StripHtml(
                            title.Groups["name"].Value));

                if (IsValidProductName(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static string CleanProductTitle(
            string value)
        {
            value =
                WebUtility.HtmlDecode(value);

            value =
                Regex.Replace(
                    value,
                    @"\s*\|\s*PnP.*$",
                    string.Empty,
                    RegexOptions.IgnoreCase);

            value =
                Regex.Replace(
                    value,
                    @"\s+",
                    " ");

            return value.Trim();
        }

        private static bool IsValidProductName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (value.Length < 3 ||
                value.Length > 250)
            {
                return false;
            }

            if (value.Equals(
                    "Pick n Pay Online Shopping",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        // =====================================================
        // VERIFIED PRICE
        // =====================================================

        private static decimal? ExtractVerifiedPrice(
            string html,
            string visibleText,
            string productName)
        {
            List<decimal> structuredPrices =
                new List<decimal>();

            // -------------------------------------------------
            // JSON-LD / structured product price
            // -------------------------------------------------

            MatchCollection jsonPriceMatches =
                Regex.Matches(
                    html,
                    @"""price""\s*:\s*[""']?(?<price>\d{1,6}(?:[.,]\d{1,2})?)[""']?",
                    RegexOptions.IgnoreCase);

            foreach (Match match in jsonPriceMatches)
            {
                decimal? parsed =
                    ParsePrice(
                        match.Groups["price"].Value);

                if (parsed.HasValue &&
                    IsReasonableGroceryPrice(parsed.Value))
                {
                    structuredPrices.Add(
                        parsed.Value);
                }
            }

            // -------------------------------------------------
            // Meta product price
            // -------------------------------------------------

            MatchCollection metaPrices =
                Regex.Matches(
                    html,
                    @"(?:product:price:amount|price)\s*[""'][^>]*content\s*=\s*[""'](?<price>\d{1,6}(?:[.,]\d{1,2})?)[""']",
                    RegexOptions.IgnoreCase);

            foreach (Match match in metaPrices)
            {
                decimal? parsed =
                    ParsePrice(
                        match.Groups["price"].Value);

                if (parsed.HasValue &&
                    IsReasonableGroceryPrice(parsed.Value))
                {
                    structuredPrices.Add(
                        parsed.Value);
                }
            }

            if (structuredPrices.Count > 0)
            {
                return structuredPrices.First();
            }

            // -------------------------------------------------
            // Visible product page price
            //
            // Product page must contain the verified product
            // name and a Rand price.
            // -------------------------------------------------

            if (!ContainsProductWords(
                    visibleText,
                    productName))
            {
                return null;
            }

            MatchCollection randPrices =
                Regex.Matches(
                    visibleText,
                    @"(?:^|\s)R\s*(?<price>\d{1,6}(?:[.,]\d{2})?)\b",
                    RegexOptions.IgnoreCase);

            foreach (Match match in randPrices)
            {
                decimal? parsed =
                    ParsePrice(
                        match.Groups["price"].Value);

                if (parsed.HasValue &&
                    IsReasonableGroceryPrice(parsed.Value))
                {
                    return parsed.Value;
                }
            }

            return null;
        }

        // =====================================================
        // PRICE PARSING
        // =====================================================

        private static decimal? ParsePrice(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized =
                value.Trim()
                    .Replace("R", "",
                        StringComparison.OrdinalIgnoreCase)
                    .Replace(" ", "")
                    .Replace(",", ".");

            if (!decimal.TryParse(
                    normalized,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out decimal price))
            {
                return null;
            }

            return price;
        }

        private static bool IsReasonableGroceryPrice(
            decimal price)
        {
            // This is validation, NOT price generation.
            // Reject impossible/obviously corrupt parsed values.
            return price > 0 &&
                   price <= 100000;
        }

        // =====================================================
        // REGULAR PRICE
        // =====================================================

        private static decimal? ExtractRegularPrice(
            string text,
            decimal currentPrice)
        {
            MatchCollection matches =
                Regex.Matches(
                    text,
                    @"R\s*(?<price>\d{1,6}(?:[.,]\d{2})?)",
                    RegexOptions.IgnoreCase);

            List<decimal> prices =
                new List<decimal>();

            foreach (Match match in matches)
            {
                decimal? parsed =
                    ParsePrice(
                        match.Groups["price"].Value);

                if (parsed.HasValue &&
                    parsed.Value > currentPrice &&
                    IsReasonableGroceryPrice(parsed.Value))
                {
                    prices.Add(
                        parsed.Value);
                }
            }

            return prices
                .Distinct()
                .OrderBy(x => x)
                .Cast<decimal?>()
                .FirstOrDefault();
        }

        // =====================================================
        // BRAND
        // =====================================================

        private static string? ExtractBrand(
            string html,
            string visibleText,
            string productName)
        {
            Match jsonBrand =
                Regex.Match(
                    html,
                    @"""brand""\s*:\s*(?:\{\s*""@type""\s*:\s*""Brand""\s*,\s*""name""\s*:\s*)?""(?<brand>[^""]+)""",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline);

            if (jsonBrand.Success)
            {
                string value =
                    WebUtility.HtmlDecode(
                        jsonBrand.Groups["brand"].Value)
                        .Trim();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            Match brandText =
                Regex.Match(
                    visibleText,
                    @"(?:Brand\s*:?\s*)(?<brand>[A-Za-z0-9&' -]{2,50})",
                    RegexOptions.IgnoreCase);

            if (brandText.Success)
            {
                string value =
                    brandText.Groups["brand"].Value
                        .Trim();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            string[] words =
                productName.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

            return words.Length > 0
                ? words[0]
                : null;
        }

        // =====================================================
        // SIZE
        // =====================================================

        private static string? ExtractSize(
            string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return null;
            }

            Match match =
                Regex.Match(
                    productName,
                    @"(?<size>\d+(?:[.,]\d+)?\s*(?:kg|g|mg|l|ml|cl|litre|litres|pack|pk))\b",
                    RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return null;
            }

            return Regex.Replace(
                    match.Groups["size"].Value,
                    @"\s+",
                    " ")
                .Trim();
        }

        // =====================================================
        // AVAILABILITY
        // =====================================================

        private static bool ContainsUnavailableText(
            string text)
        {
            return
                text.Contains(
                    "Currently unavailable",
                    StringComparison.OrdinalIgnoreCase)
                ||
                text.Contains(
                    "Currently out of stock",
                    StringComparison.OrdinalIgnoreCase)
                ||
                text.Contains(
                    "Out of stock",
                    StringComparison.OrdinalIgnoreCase)
                ||
                text.Contains(
                    "Not available",
                    StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================
        // PROMOTION
        // =====================================================

        private static bool ContainsPromotionText(
            string text)
        {
            return
                text.Contains(
                    "On Promotion",
                    StringComparison.OrdinalIgnoreCase)
                ||
                text.Contains(
                    "Smart Shopper",
                    StringComparison.OrdinalIgnoreCase)
                ||
                text.Contains(
                    "Smart Price",
                    StringComparison.OrdinalIgnoreCase)
                ||
                text.Contains(
                    "SAVE",
                    StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================
        // MATCH SCORE
        // =====================================================

        private static double CalculateMatchScore(
            string searchTerm,
            string productName)
        {
            string normalizedSearch =
                NormalizeText(
                    searchTerm);

            string normalizedProduct =
                NormalizeText(
                    productName);

            if (string.IsNullOrWhiteSpace(normalizedSearch) ||
                string.IsNullOrWhiteSpace(normalizedProduct))
            {
                return 0;
            }

            HashSet<string> searchWords =
                GetMeaningfulWords(
                    searchTerm);

            HashSet<string> productWords =
                GetMeaningfulWords(
                    productName);

            if (searchWords.Count == 0 ||
                productWords.Count == 0)
            {
                return 0;
            }

            int matchedWords =
                searchWords.Count(
                    word =>
                        productWords.Contains(word));

            double wordScore =
                (double)matchedWords /
                searchWords.Count;

            double containsBonus =
                normalizedProduct.Contains(
                    normalizedSearch,
                    StringComparison.OrdinalIgnoreCase)
                    ? 0.25
                    : 0;

            // Give extra importance to pack/size tokens.
            List<string> searchSizes =
                ExtractSizeTokens(
                    searchTerm);

            List<string> productSizes =
                ExtractSizeTokens(
                    productName);

            double sizeAdjustment = 0;

            if (searchSizes.Count > 0)
            {
                bool sizeMatches =
                    searchSizes.Any(
                        searchSize =>
                            productSizes.Contains(
                                searchSize,
                                StringComparer.OrdinalIgnoreCase));

                sizeAdjustment =
                    sizeMatches
                        ? 0.20
                        : -0.30;
            }

            double score =
                wordScore +
                containsBonus +
                sizeAdjustment;

            return Math.Clamp(
                score,
                0,
                1);
        }

        private static List<string> ExtractSizeTokens(
            string value)
        {
            MatchCollection matches =
                Regex.Matches(
                    value,
                    @"\d+(?:[.,]\d+)?\s*(?:kg|g|mg|l|ml|cl|litre|litres|pack|pk)\b",
                    RegexOptions.IgnoreCase);

            return matches
                .Select(match =>
                    NormalizeText(
                        match.Value))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // =====================================================
        // SEARCH URL PRIORITISATION
        // =====================================================

        private static bool IsReasonableProductUrlForSearch(
            string url,
            string searchTerm)
        {
            string urlName =
                GetProductNameFromUrl(
                    url);

            if (string.IsNullOrWhiteSpace(urlName))
            {
                return false;
            }

            return CalculateMatchScore(
                       searchTerm,
                       urlName) >= 0.35;
        }

        // =====================================================
        // PRODUCT NAME FROM URL
        // =====================================================

        private static string GetProductNameFromUrl(
            string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            string path =
                url;

            if (Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out Uri? absoluteUri))
            {
                path =
                    absoluteUri.AbsolutePath;
            }

            int marker =
                path.IndexOf(
                    "/p/",
                    StringComparison.OrdinalIgnoreCase);

            if (marker > 0)
            {
                path =
                    path.Substring(
                        0,
                        marker);
            }

            path =
                path.Trim('/');

            int slash =
                path.LastIndexOf('/');

            if (slash >= 0)
            {
                path =
                    path.Substring(
                        slash + 1);
            }

            path =
                Uri.UnescapeDataString(
                    path);

            path =
                path.Replace(
                    "-",
                    " ");

            path =
                Regex.Replace(
                    path,
                    @"\s+",
                    " ");

            return ToDisplayName(
                path.Trim());
        }

        // =====================================================
        // WORD HELPERS
        // =====================================================

        private static HashSet<string> GetMeaningfulWords(
            string value)
        {
            string normalized =
                NormalizeText(
                    value);

            return normalized
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(word =>
                    word.Length >= 2)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeText(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalized =
                value.ToLowerInvariant();

            // Make common size formats comparable:
            // 2 L == 2L
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

        private static bool ContainsProductWords(
            string pageText,
            string productName)
        {
            HashSet<string> productWords =
                GetMeaningfulWords(
                    productName);

            if (productWords.Count == 0)
            {
                return false;
            }

            string normalizedPage =
                NormalizeText(
                    pageText);

            int found =
                productWords.Count(
                    word =>
                        normalizedPage.Contains(
                            word,
                            StringComparison.OrdinalIgnoreCase));

            return found >=
                   Math.Max(
                       1,
                       (int)Math.Ceiling(
                           productWords.Count * 0.60));
        }

        // =====================================================
        // HTML HELPERS
        // =====================================================

        private static string StripNonVisibleContent(
            string html)
        {
            string value =
                Regex.Replace(
                    html,
                    @"<script\b[^>]*>.*?</script>",
                    " ",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline);

            value =
                Regex.Replace(
                    value,
                    @"<style\b[^>]*>.*?</style>",
                    " ",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline);

            value =
                Regex.Replace(
                    value,
                    @"<noscript\b[^>]*>.*?</noscript>",
                    " ",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline);

            return StripHtml(
                value);
        }

        private static string StripHtml(
            string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            string text =
                Regex.Replace(
                    html,
                    @"<[^>]+>",
                    " ");

            text =
                WebUtility.HtmlDecode(
                    text);

            text =
                Regex.Replace(
                    text,
                    @"\s+",
                    " ");

            return text.Trim();
        }

        // =====================================================
        // DISPLAY NAME
        // =====================================================

        private static string ToDisplayName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            TextInfo textInfo =
                CultureInfo
                    .InvariantCulture
                    .TextInfo;

            return textInfo.ToTitleCase(
                value.ToLowerInvariant());
        }

        // =====================================================
        // INTERNAL PRODUCT CANDIDATE
        // =====================================================

        private sealed class ProductCandidate
        {
            public string ProductName { get; set; } =
                string.Empty;

            public string? Brand { get; set; }

            public string? Size { get; set; }

            public decimal Price { get; set; }

            public decimal? RegularPrice { get; set; }

            public bool IsOnPromotion { get; set; }

            public bool IsAvailable { get; set; }

            public string ProductUrl { get; set; } =
                string.Empty;

            public double MatchScore { get; set; }
        }
    }
}