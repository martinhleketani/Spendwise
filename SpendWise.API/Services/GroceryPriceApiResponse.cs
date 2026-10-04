using System.Text.Json.Serialization;

namespace SpendWise.API.Services
{
    // =========================================================
    // GROCERY PRICE API RESPONSE
    // =========================================================
    //
    // Represents the response returned by the external
    // grocery-price service.
    //
    // These classes are API transport models only.
    // They are NOT Entity Framework database entities.
    // =========================================================

    public class GroceryPriceApiResponse
    {
        [JsonPropertyName("data")]
        public List<GroceryPriceApiProduct> Data { get; set; } =
            new();

        [JsonPropertyName("meta")]
        public GroceryPriceApiMeta? Meta { get; set; }
    }


    // =========================================================
    // RESPONSE META DATA
    // =========================================================

    public class GroceryPriceApiMeta
    {
        [JsonPropertyName("count")]
        public int? Count { get; set; }

        [JsonPropertyName("total")]
        public int? Total { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }

        [JsonPropertyName("offset")]
        public int? Offset { get; set; }

        [JsonPropertyName("has_more")]
        public bool? HasMore { get; set; }

        [JsonPropertyName("next_offset")]
        public int? NextOffset { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("max_age_days")]
        public int? MaxAgeDays { get; set; }

        [JsonPropertyName("quota")]
        public int? Quota { get; set; }

        [JsonPropertyName("used")]
        public int? Used { get; set; }
    }


    // =========================================================
    // PRODUCT
    // =========================================================

    public class GroceryPriceApiProduct
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("retailer")]
        public string? Retailer { get; set; }

        [JsonPropertyName("retailer_name")]
        public string? RetailerName { get; set; }

        [JsonPropertyName("retailer_sku")]
        public string? RetailerSku { get; set; }

        [JsonPropertyName("barcode")]
        public string? Barcode { get; set; }

        [JsonPropertyName("gtin")]
        public string? Gtin { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("regular_price")]
        public decimal? RegularPrice { get; set; }

        [JsonPropertyName("sale_price")]
        public decimal? SalePrice { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("unit_size")]
        public string? UnitSize { get; set; }

        // IMPORTANT:
        // The real API returns:
        //
        // "size": {
        //     "quantity": 0.33,
        //     "unit": "l"
        // }
        //
        // Therefore Size must be an object, not a string.

        [JsonPropertyName("size")]
        public GroceryPriceApiSize? Size { get; set; }

        [JsonPropertyName("unit_price")]
        public GroceryPriceApiUnitPrice? UnitPrice { get; set; }

        [JsonPropertyName("in_stock")]
        public bool? InStock { get; set; }

        [JsonPropertyName("is_available")]
        public bool? IsAvailable { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("department")]
        public string? Department { get; set; }

        [JsonPropertyName("promotion")]
        public bool? Promotion { get; set; }

        [JsonPropertyName("is_on_promotion")]
        public bool? IsOnPromotion { get; set; }

        [JsonPropertyName("product_url")]
        public string? ProductUrl { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("last_seen_at")]
        public DateTime? LastSeenAt { get; set; }

        [JsonPropertyName("price_changed_at")]
        public DateTime? PriceChangedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("price_updated_at")]
        public DateTime? PriceUpdatedAt { get; set; }
    }


    // =========================================================
    // PRODUCT SIZE
    // =========================================================

    public class GroceryPriceApiSize
    {
        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
    }


    // =========================================================
    // UNIT PRICE
    // =========================================================

    public class GroceryPriceApiUnitPrice
    {
        [JsonPropertyName("value")]
        public decimal? Value { get; set; }

        [JsonPropertyName("per")]
        public string? Per { get; set; }
    }
}