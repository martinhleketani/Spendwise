using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendWise.API.Services;

namespace SpendWise.API.Controllers
{
    // =========================================================
    // GROCERY PRICE API TEST CONTROLLER
    // =========================================================
    //
    // Temporary development endpoint used to verify that
    // SpendWise can communicate with the external grocery
    // price service.
    //
    // This does NOT save anything to the database.
    // =========================================================

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroceryPriceTestController : ControllerBase
    {
        private readonly GroceryPriceApiService
            _groceryPriceApiService;

        private readonly ILogger<GroceryPriceTestController>
            _logger;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public GroceryPriceTestController(
            GroceryPriceApiService groceryPriceApiService,
            ILogger<GroceryPriceTestController> logger)
        {
            _groceryPriceApiService =
                groceryPriceApiService;

            _logger =
                logger;
        }


        // =====================================================
        // TEST PRODUCT SEARCH
        // =====================================================
        //
        // GET:
        //
        // /api/GroceryPriceTest/search
        //     ?product=Coca-Cola%202L
        //
        // =====================================================

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string product,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(product))
            {
                return BadRequest(
                    new
                    {
                        success = false,

                        message =
                            "Enter a product name to search."
                    });
            }


            string searchTerm =
                product.Trim();


            try
            {
                var results =
                    await _groceryPriceApiService
                        .SearchProductAcrossRetailersAsync(
                            searchTerm,
                            cancellationToken);


                var response =
                    results
                        .Select(
                            result =>
                                new
                                {
                                    retailer =
                                        result.Key,

                                    priceFound =
                                        result.Value != null &&
                                        result.Value.Price > 0,

                                    productName =
                                        result.Value?.ProductName,

                                    brand =
                                        result.Value?.Brand,

                                    size =
                                        result.Value?.Size,

                                    price =
                                        result.Value?.Price,

                                    regularPrice =
                                        result.Value?.RegularPrice,

                                    isOnPromotion =
                                        result.Value?.IsOnPromotion,

                                    isAvailable =
                                        result.Value?.IsAvailable,

                                    productUrl =
                                        result.Value?.ProductUrl,

                                    priceCheckedAt =
                                        result.Value?.PriceCheckedAt,

                                    priceSource =
                                        result.Value?.PriceSource
                                })
                        .ToList();


                int pricesFound =
                    response.Count(
                        result =>
                            result.priceFound);


                return Ok(
                    new
                    {
                        success = true,

                        searchTerm,

                        supportedRetailers =
                            results.Count,

                        pricesFound,

                        pricesMissing =
                            results.Count -
                            pricesFound,

                        results =
                            response
                    });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(
                    StatusCodes
                        .Status408RequestTimeout,

                    new
                    {
                        success = false,

                        message =
                            "The grocery price request was cancelled or timed out."
                    });
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Grocery price test failed for {Product}.",
                    searchTerm);


                return StatusCode(
                    StatusCodes
                        .Status500InternalServerError,

                    new
                    {
                        success = false,

                        message =
                            "The grocery price test failed.",

                        error =
                            exception.Message
                    });
            }
        }
    }
}