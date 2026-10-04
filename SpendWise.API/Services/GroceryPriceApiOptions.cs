namespace SpendWise.API.Services
{
    /// <summary>
    /// Configuration used by SpendWise to connect to the
    /// external grocery-price service.
    ///
    /// The API key itself is stored in ASP.NET Core
    /// User Secrets during development and must never be
    /// hard-coded into this source file.
    /// </summary>
    public class GroceryPriceApiOptions
    {
        public const string SectionName = "LoyaltyHub";

        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } =
            "https://loyaltyhub.co.za";
    }
}