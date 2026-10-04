using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.DTOs;
using SpendWise.API.Models;
using SpendWise.API.Services.Retailers;
using System.Security.Claims;

namespace SpendWise.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroceryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PriceComparisonService _priceComparisonService;

        private static readonly string[] AllowedRetailers =
        {
            "Pick n Pay",
            "Shoprite",
            "Checkers",
            "SPAR",
            "Woolworths Food",
            "Other"
        };


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public GroceryController(
            AppDbContext context,
            PriceComparisonService priceComparisonService)
        {
            _context = context;
            _priceComparisonService = priceComparisonService;
        }


        // =====================================================
        // CREATE GROCERY LIST
        // POST: /api/Grocery
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> CreateList(
            CreateGroceryListDto request)
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }


            if (string.IsNullOrWhiteSpace(
                    request.ListName))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "List name is required."
                    });
            }


            if (string.IsNullOrWhiteSpace(
                    request.Retailer))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Retailer is required."
                    });
            }


            string? retailer =
                AllowedRetailers
                    .FirstOrDefault(
                        r =>
                            r.Equals(
                                request.Retailer.Trim(),
                                StringComparison.OrdinalIgnoreCase));


            if (retailer == null)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Invalid retailer.",

                        allowedRetailers =
                            AllowedRetailers
                    });
            }


            var groceryList =
                new GroceryList
                {
                    UserId =
                        userId.Value,

                    ListName =
                        request.ListName.Trim(),

                    Retailer =
                        retailer,

                    Status =
                        "Active",

                    CreatedAt =
                        DateTime.UtcNow,

                    UpdatedAt =
                        DateTime.UtcNow
                };


            _context
                .GroceryLists
                .Add(
                    groceryList);


            await _context
                .SaveChangesAsync();


            return Ok(
                new
                {
                    message =
                        "Grocery list created successfully.",

                    groceryList.Id,

                    groceryList.ListName,

                    groceryList.Retailer,

                    groceryList.Status,

                    groceryList.CreatedAt
                });
        }


        // =====================================================
        // GET ALL GROCERY LISTS
        // GET: /api/Grocery
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetLists()
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }


            var lists =
                await _context
                    .GroceryLists
                    .Where(
                        g =>
                            g.UserId ==
                                userId.Value)
                    .Include(
                        g =>
                            g.Items)
                    .OrderByDescending(
                        g =>
                            g.CreatedAt)
                    .Select(
                        g =>
                            new
                            {
                                g.Id,

                                g.ListName,

                                g.Retailer,

                                g.Status,

                                itemCount =
                                    g.Items.Count,

                                total =
                                    g.Items.Sum(
                                        i =>
                                            i.UnitPrice *
                                            i.Quantity),

                                g.CreatedAt,

                                g.UpdatedAt
                            })
                    .ToListAsync();


            return Ok(
                lists);
        }


        // =====================================================
        // GET ONE GROCERY LIST
        // GET: /api/Grocery/{id}
        // =====================================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetList(
            int id)
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }


            var groceryList =
                await _context
                    .GroceryLists
                    .Include(
                        g =>
                            g.Items)
                    .FirstOrDefaultAsync(
                        g =>
                            g.Id ==
                                id &&
                            g.UserId ==
                                userId.Value);


            if (groceryList == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Grocery list not found."
                    });
            }


            decimal total =
                groceryList
                    .Items
                    .Sum(
                        i =>
                            i.UnitPrice *
                            i.Quantity);


            return Ok(
                new
                {
                    groceryList.Id,

                    groceryList.ListName,

                    groceryList.Retailer,

                    groceryList.Status,

                    itemCount =
                        groceryList.Items.Count,

                    total,

                    items =
                        groceryList
                            .Items
                            .Select(
                                i =>
                                    new
                                    {
                                        i.Id,

                                        i.ProductName,

                                        i.Quantity,

                                        i.UnitPrice,

                                        totalPrice =
                                            i.Quantity *
                                            i.UnitPrice,

                                        i.IsPurchased,

                                        i.CreatedAt
                                    }),

                    groceryList.CreatedAt,

                    groceryList.UpdatedAt
                });
        }


        // =====================================================
        // ADD ITEM
        // POST: /api/Grocery/{id}/items
        // =====================================================

        [HttpPost("{id:int}/items")]
        public async Task<IActionResult> AddItem(
            int id,
            AddGroceryItemDto request)
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }


            var groceryList =
                await _context
                    .GroceryLists
                    .Include(
                        g =>
                            g.Items)
                    .FirstOrDefaultAsync(
                        g =>
                            g.Id ==
                                id &&
                            g.UserId ==
                                userId.Value);


            if (groceryList == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Grocery list not found."
                    });
            }


            if (!groceryList.Status.Equals(
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Items can only be added to an active grocery list."
                    });
            }


            if (string.IsNullOrWhiteSpace(
                    request.ProductName))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Product name is required."
                    });
            }


            if (request.Quantity <= 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Quantity must be greater than 0."
                    });
            }


            if (request.UnitPrice <= 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Unit price must be greater than R0."
                    });
            }


            var item =
                new GroceryItem
                {
                    GroceryListId =
                        groceryList.Id,

                    ProductName =
                        request.ProductName.Trim(),

                    Quantity =
                        request.Quantity,

                    UnitPrice =
                        request.UnitPrice,

                    IsPurchased =
                        false,

                    CreatedAt =
                        DateTime.UtcNow
                };


            _context
                .GroceryItems
                .Add(
                    item);


            groceryList.UpdatedAt =
                DateTime.UtcNow;


            await _context
                .SaveChangesAsync();


            decimal listTotal =
                groceryList
                    .Items
                    .Sum(
                        i =>
                            i.UnitPrice *
                            i.Quantity);


            return Ok(
                new
                {
                    message =
                        "Item added successfully.",

                    item =
                        new
                        {
                            item.Id,

                            item.ProductName,

                            item.Quantity,

                            item.UnitPrice,

                            totalPrice =
                                item.Quantity *
                                item.UnitPrice,

                            item.IsPurchased
                        },

                    listTotal
                });
        }


        // =====================================================
        // CHECK GROCERY LIST AGAINST BUDGET
        // POST: /api/Grocery/{id}/budget-check
        // =====================================================

        [HttpPost("{id:int}/budget-check")]
        public async Task<IActionResult> CheckBudget(
            int id,
            GroceryBudgetCheckDto request)
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }


            if (request.Month < 1 ||
                request.Month > 12)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Month must be between 1 and 12."
                    });
            }


            if (request.Year < 2020 ||
                request.Year > 2100)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Invalid year."
                    });
            }


            var groceryList =
                await _context
                    .GroceryLists
                    .Include(
                        g =>
                            g.Items)
                    .FirstOrDefaultAsync(
                        g =>
                            g.Id ==
                                id &&
                            g.UserId ==
                                userId.Value);


            if (groceryList == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Grocery list not found."
                    });
            }


            var monthlyBudget =
                await _context
                    .MonthlyBudgets
                    .Include(
                        b =>
                            b.CategoryBudgets)
                    .FirstOrDefaultAsync(
                        b =>
                            b.UserId ==
                                userId.Value &&
                            b.Month ==
                                request.Month &&
                            b.Year ==
                                request.Year);


            if (monthlyBudget == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "No monthly budget was found for the selected month and year."
                    });
            }


            var groceryBudget =
                monthlyBudget
                    .CategoryBudgets
                    .FirstOrDefault(
                        c =>
                            c.Category.Equals(
                                "Groceries",
                                StringComparison.OrdinalIgnoreCase));


            if (groceryBudget == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "No Groceries category exists in this monthly budget."
                    });
            }


            decimal listTotal =
                groceryList
                    .Items
                    .Sum(
                        i =>
                            i.UnitPrice *
                            i.Quantity);


            decimal groceryLimit =
                groceryBudget.LimitAmount;


            decimal alreadySpent =
                groceryBudget.SpentAmount;


            decimal availableBeforeList =
                groceryLimit -
                alreadySpent;


            decimal remainingAfterList =
                availableBeforeList -
                listTotal;


            bool withinBudget =
                remainingAfterList >= 0;


            decimal overBudgetBy =
                remainingAfterList < 0
                    ? Math.Abs(
                        remainingAfterList)
                    : 0;


            return Ok(
                new
                {
                    groceryListId =
                        groceryList.Id,

                    groceryListName =
                        groceryList.ListName,

                    retailer =
                        groceryList.Retailer,

                    month =
                        request.Month,

                    year =
                        request.Year,

                    groceryBudgetLimit =
                        groceryLimit,

                    alreadySpent,

                    availableBeforeList,

                    listTotal,

                    remainingAfterList,

                    withinBudget,

                    overBudgetBy,

                    status =
                        withinBudget
                            ? "Within budget"
                            : "Over budget"
                });
        }


        // =====================================================
        // RECORD GROCERY PURCHASE
        // POST: /api/Grocery/{id}/checkout
        // =====================================================

        [HttpPost("{id:int}/checkout")]
        public async Task<IActionResult> Checkout(
            int id,
            CheckoutDto request)
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }


            if (request.Month < 1 ||
                request.Month > 12)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Month must be between 1 and 12."
                    });
            }


            if (request.Year < 2020 ||
                request.Year > 2100)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Invalid year."
                    });
            }


            var groceryList =
                await _context
                    .GroceryLists
                    .Include(
                        g =>
                            g.Items)
                    .FirstOrDefaultAsync(
                        g =>
                            g.Id ==
                                id &&
                            g.UserId ==
                                userId.Value);


            if (groceryList == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Grocery list not found."
                    });
            }


            if (!groceryList.Status.Equals(
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "This grocery list has already been completed or is not active."
                    });
            }


            if (groceryList.Items.Count == 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Cannot record an empty grocery list."
                    });
            }


            decimal listTotal =
                groceryList
                    .Items
                    .Sum(
                        i =>
                            i.UnitPrice *
                            i.Quantity);


            if (listTotal <= 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "The grocery list total must be greater than R0."
                    });
            }


            var monthlyBudget =
                await _context
                    .MonthlyBudgets
                    .Include(
                        b =>
                            b.CategoryBudgets)
                    .FirstOrDefaultAsync(
                        b =>
                            b.UserId ==
                                userId.Value &&
                            b.Month ==
                                request.Month &&
                            b.Year ==
                                request.Year);


            if (monthlyBudget == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "No monthly budget was found for the selected month and year."
                    });
            }


            var groceryBudget =
                monthlyBudget
                    .CategoryBudgets
                    .FirstOrDefault(
                        c =>
                            c.Category.Equals(
                                "Groceries",
                                StringComparison.OrdinalIgnoreCase));


            if (groceryBudget == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "No Groceries category exists in this monthly budget."
                    });
            }


            decimal groceryAvailable =
                groceryBudget.LimitAmount -
                groceryBudget.SpentAmount;


            bool withinBudget =
                listTotal <=
                groceryAvailable;


            decimal projectedGrocerySpent =
                groceryBudget.SpentAmount +
                listTotal;


            decimal projectedBudgetRemaining =
                groceryBudget.LimitAmount -
                projectedGrocerySpent;


            await using var databaseTransaction =
                await _context
                    .Database
                    .BeginTransactionAsync();


            try
            {
                groceryBudget.SpentAmount +=
                    listTotal;


                foreach (
                    var item
                    in groceryList.Items)
                {
                    item.IsPurchased =
                        true;
                }


                groceryList.Status =
                    "Completed";


                groceryList.UpdatedAt =
                    DateTime.UtcNow;


                var spendingRecord =
                    new Transaction
                    {
                        UserId =
                            userId.Value,

                        Amount =
                            -listTotal,

                        Type =
                            "Purchase",

                        Category =
                            "Groceries",

                        Retailer =
                            groceryList.Retailer,

                        Description =
                            $"Grocery purchase at {groceryList.Retailer}",

                        Reference =
                            $"GROCERY-{groceryList.Id}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",

                        CreatedAt =
                            DateTime.UtcNow
                    };


                _context
                    .Transactions
                    .Add(
                        spendingRecord);


                await _context
                    .SaveChangesAsync();


                await databaseTransaction
                    .CommitAsync();


                return Ok(
                    new
                    {
                        message =
                            "Grocery purchase recorded successfully. No payment was processed by SpendWise.",

                        groceryListId =
                            groceryList.Id,

                        groceryListName =
                            groceryList.ListName,

                        retailer =
                            groceryList.Retailer,

                        purchaseTotal =
                            listTotal,

                        groceryBudgetLimit =
                            groceryBudget
                                .LimitAmount,

                        grocerySpent =
                            groceryBudget
                                .SpentAmount,

                        groceryBudgetRemaining =
                            projectedBudgetRemaining,

                        withinBudget,

                        budgetStatus =
                            withinBudget
                                ? "Within budget"
                                : "Over budget",

                        transactionReference =
                            spendingRecord.Reference,

                        status =
                            groceryList.Status,

                        paymentProcessed =
                            false
                    });
            }
            catch (DbUpdateException)
            {
                await databaseTransaction
                    .RollbackAsync();


                _context
                    .ChangeTracker
                    .Clear();


                return StatusCode(
                    StatusCodes
                        .Status500InternalServerError,

                    new
                    {
                        message =
                            "Recording the grocery purchase failed. No changes were completed."
                    });
            }
            catch
            {
                await databaseTransaction
                    .RollbackAsync();


                _context
                    .ChangeTracker
                    .Clear();


                return StatusCode(
                    StatusCodes
                        .Status500InternalServerError,

                    new
                    {
                        message =
                            "Recording the grocery purchase failed. No changes were completed."
                    });
            }
        }


        // =====================================================
        // DELETE ITEM FROM GROCERY LIST
        // DELETE: /api/Grocery/{listId}/items/{itemId}
        // =====================================================

        [HttpDelete(
            "{listId:int}/items/{itemId:int}")]
        public async Task<IActionResult> DeleteItem(
            int listId,
            int itemId)
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }


            var groceryList =
                await _context
                    .GroceryLists
                    .FirstOrDefaultAsync(
                        g =>
                            g.Id ==
                                listId &&
                            g.UserId ==
                                userId.Value);


            if (groceryList == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Grocery list not found."
                    });
            }


            if (!groceryList.Status.Equals(
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Items cannot be removed from a completed grocery list."
                    });
            }


            var item =
                await _context
                    .GroceryItems
                    .FirstOrDefaultAsync(
                        i =>
                            i.Id ==
                                itemId &&
                            i.GroceryListId ==
                                listId);


            if (item == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Grocery item not found."
                    });
            }


            _context
                .GroceryItems
                .Remove(
                    item);


            groceryList.UpdatedAt =
                DateTime.UtcNow;


            await _context
                .SaveChangesAsync();


            return Ok(
                new
                {
                    message =
                        "Item removed successfully."
                });
        }


        // =====================================================
        // DELETE ENTIRE GROCERY LIST
        // DELETE: /api/Grocery/{id}
        // =====================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteList(
            int id)
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }


            var groceryList =
                await _context
                    .GroceryLists
                    .FirstOrDefaultAsync(
                        g =>
                            g.Id ==
                                id &&
                            g.UserId ==
                                userId.Value);


            if (groceryList == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Grocery list not found."
                    });
            }


            if (!groceryList.Status.Equals(
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "A completed grocery list cannot be deleted."
                    });
            }


            _context
                .GroceryLists
                .Remove(
                    groceryList);


            await _context
                .SaveChangesAsync();


            return Ok(
                new
                {
                    message =
                        "Grocery list deleted successfully."
                });
        }


        // =====================================================
        // RETAILER PRICE COMPARISON
        // GET: /api/Grocery/{id}/price-comparison
        // =====================================================

        [HttpGet("{id:int}/price-comparison")]
        public async Task<IActionResult> GetPriceComparison(
            int id,
            CancellationToken cancellationToken)
        {
            int? userId =
                GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }


            // =================================================
            // GET USER'S GROCERY LIST
            // =================================================

            var groceryList =
                await _context
                    .GroceryLists
                    .AsNoTracking()
                    .Include(
                        g =>
                            g.Items)
                    .FirstOrDefaultAsync(
                        g =>
                            g.Id ==
                                id &&
                            g.UserId ==
                                userId.Value,
                        cancellationToken);


            if (groceryList == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Grocery list not found."
                    });
            }


            // =================================================
            // CHECK THAT LIST HAS ITEMS
            // =================================================

            if (groceryList.Items.Count == 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Add at least one item before comparing retailer prices."
                    });
            }


            // =================================================
            // CONVERT COLLECTION TO LIST
            // =================================================
            //
            // GroceryList.Items is ICollection<GroceryItem>.
            // ICollection cannot be accessed using [index].
            // Converting it to a List fixes CS0021.
            // =================================================

            List<GroceryItem> groceryItems =
                groceryList
                    .Items
                    .ToList();


            // =================================================
            // GET LIVE RETAILER RESULTS
            // =================================================

            Dictionary<
                string,
                List<RetailerProductResult?>>
                providerResults =
                    await _priceComparisonService
                        .CompareGroceryListAsync(
                            groceryItems,
                            cancellationToken);


            // =================================================
            // GET RETAILER DATABASE RECORDS
            // =================================================

            var retailerRecords =
                await _context
                    .Retailers
                    .AsNoTracking()
                    .Where(
                        r =>
                            r.IsActive)
                    .ToListAsync(
                        cancellationToken);


            // =================================================
            // CREATE RESPONSE
            // =================================================

            var comparison =
                new RetailerPriceComparisonDto
                {
                    GroceryListId =
                        groceryList.Id,

                    GroceryListName =
                        groceryList.ListName,

                    GeneratedAt =
                        DateTime.UtcNow,

                    Retailers =
                        new List<RetailerComparisonDto>()
                };


            // =================================================
            // PROCESS EACH REGISTERED PROVIDER
            // =================================================

            foreach (
                KeyValuePair<
                    string,
                    List<RetailerProductResult?>>
                    providerEntry
                in providerResults)
            {
                string retailerName =
                    providerEntry.Key;


                List<RetailerProductResult?>
                    productResults =
                        providerEntry.Value;


                var retailerRecord =
                    retailerRecords
                        .FirstOrDefault(
                            r =>
                                r.Name.Equals(
                                    retailerName,
                                    StringComparison.OrdinalIgnoreCase));


                var retailerResult =
                    new RetailerComparisonDto
                    {
                        RetailerId =
                            retailerRecord?.Id ?? 0,

                        RetailerName =
                            retailerName,

                        EstimatedTotal =
                            0,

                        MatchedItemCount =
                            0,

                        MissingItemCount =
                            0,

                        HasCompletePricing =
                            false,

                        LatestPriceCheckedAt =
                            null,

                        Items =
                            new List<RetailerComparisonItemDto>()
                    };


                decimal estimatedTotal =
                    0;


                int matchedItemCount =
                    0;


                int missingItemCount =
                    0;


                DateTime? latestPriceCheckedAt =
                    null;


                // =============================================
                // MATCH RESULTS BY ITEM POSITION
                // =============================================

                for (
                    int index = 0;
                    index < groceryItems.Count;
                    index++)
                {
                    GroceryItem groceryItem =
                        groceryItems[index];


                    RetailerProductResult?
                        productResult =
                            index < productResults.Count
                                ? productResults[index]
                                : null;


                    // =========================================
                    // NO RELIABLE PRICE
                    // =========================================

                    if (productResult == null ||
                        productResult.Price <= 0 ||
                        !productResult.IsAvailable)
                    {
                        missingItemCount++;


                        retailerResult
                            .Items
                            .Add(
                                new RetailerComparisonItemDto
                                {
                                    ProductName =
                                        groceryItem.ProductName,

                                    Quantity =
                                        groceryItem.Quantity,

                                    PriceFound =
                                        false,

                                    UnitPrice =
                                        null,

                                    RegularPrice =
                                        null,

                                    TotalPrice =
                                        null,

                                    MatchedProductName =
                                        productResult?.ProductName,

                                    Brand =
                                        productResult?.Brand,

                                    Size =
                                        productResult?.Size,

                                    IsOnPromotion =
                                        false,

                                    ProductUrl =
                                        productResult?.ProductUrl,

                                    PriceCheckedAt =
                                        productResult?.PriceCheckedAt,

                                    PriceSource =
                                        productResult?.PriceSource
                                });


                        continue;
                    }


                    // =========================================
                    // RELIABLE PRICE FOUND
                    // =========================================

                    decimal itemTotal =
                        productResult.Price *
                        groceryItem.Quantity;


                    estimatedTotal +=
                        itemTotal;


                    matchedItemCount++;


                    if (latestPriceCheckedAt == null ||
                        productResult.PriceCheckedAt >
                            latestPriceCheckedAt.Value)
                    {
                        latestPriceCheckedAt =
                            productResult.PriceCheckedAt;
                    }


                    retailerResult
                        .Items
                        .Add(
                            new RetailerComparisonItemDto
                            {
                                ProductName =
                                    groceryItem.ProductName,

                                Quantity =
                                    groceryItem.Quantity,

                                PriceFound =
                                    true,

                                UnitPrice =
                                    productResult.Price,

                                RegularPrice =
                                    productResult.RegularPrice,

                                TotalPrice =
                                    itemTotal,

                                MatchedProductName =
                                    productResult.ProductName,

                                Brand =
                                    productResult.Brand,

                                Size =
                                    productResult.Size,

                                IsOnPromotion =
                                    productResult.IsOnPromotion,

                                ProductUrl =
                                    productResult.ProductUrl,

                                PriceCheckedAt =
                                    productResult.PriceCheckedAt,

                                PriceSource =
                                    productResult.PriceSource
                            });
                }


                // =============================================
                // RETAILER TOTALS
                // =============================================

                retailerResult.EstimatedTotal =
                    estimatedTotal;


                retailerResult.MatchedItemCount =
                    matchedItemCount;


                retailerResult.MissingItemCount =
                    missingItemCount;


                retailerResult.HasCompletePricing =
                    groceryItems.Count > 0 &&
                    missingItemCount == 0;


                retailerResult.LatestPriceCheckedAt =
                    latestPriceCheckedAt;


                comparison
                    .Retailers
                    .Add(
                        retailerResult);
            }


            // =================================================
            // ORDER RESULTS
            // =================================================

            comparison.Retailers =
                comparison
                    .Retailers
                    .OrderByDescending(
                        r =>
                            r.HasCompletePricing)
                    .ThenByDescending(
                        r =>
                            r.MatchedItemCount)
                    .ThenBy(
                        r =>
                            r.HasCompletePricing
                                ? r.EstimatedTotal
                                : decimal.MaxValue)
                    .ThenBy(
                        r =>
                            r.RetailerName)
                    .ToList();


            return Ok(
                comparison);
        }


        // =====================================================
        // GET CURRENT LOGGED-IN USER
        // =====================================================

        private int? GetCurrentUserId()
        {
            string? value =
                User.FindFirstValue(
                    ClaimTypes
                        .NameIdentifier);


            if (!int.TryParse(
                    value,
                    out int userId))
            {
                return null;
            }


            return userId;
        }
    }
}