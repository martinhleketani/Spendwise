using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.DTOs;
using SpendWise.API.Models;
using System.Security.Claims;

namespace SpendWise.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PurchaseController : ControllerBase
    {
        private readonly AppDbContext _context;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public PurchaseController(
            AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // CREATE / RECORD GENERAL PURCHASE
        //
        // POST: /api/Purchase
        //
        // This endpoint records spending only.
        // It does NOT process a real bank payment.
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> CreatePurchase(
            CreatePurchaseDto request)
        {
            int? userId =
                GetCurrentUserId();


            if (userId == null)
            {
                return Unauthorized();
            }


            // =================================================
            // VALIDATE CATEGORY
            // =================================================

            if (string.IsNullOrWhiteSpace(
                    request.Category))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Category is required."
                    });
            }


            // =================================================
            // VALIDATE DESCRIPTION
            // =================================================

            if (string.IsNullOrWhiteSpace(
                    request.Description))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Description is required."
                    });
            }


            // =================================================
            // VALIDATE AMOUNT
            // =================================================

            if (request.Amount <= 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Purchase amount must be greater than R0."
                    });
            }


            // =================================================
            // VALIDATE MONTH
            // =================================================

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


            // =================================================
            // VALIDATE YEAR
            // =================================================

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


            // =================================================
            // VALIDATE REQUEST ID
            // =================================================

            if (string.IsNullOrWhiteSpace(
                    request.RequestId))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "RequestId is required."
                    });
            }


            string cleanRequestId =
                request.RequestId.Trim();

            string cleanCategory =
                request.Category.Trim();

            string cleanDescription =
                request.Description.Trim();


            // =================================================
            // FIND MONTHLY BUDGET
            // =================================================

            var monthlyBudget =
                await _context
                    .MonthlyBudgets
                    .Include(
                        budget =>
                            budget.CategoryBudgets)
                    .FirstOrDefaultAsync(
                        budget =>
                            budget.UserId ==
                                userId.Value &&
                            budget.Month ==
                                request.Month &&
                            budget.Year ==
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


            // =================================================
            // FIND CATEGORY
            // =================================================

            var categoryBudget =
                monthlyBudget
                    .CategoryBudgets
                    .FirstOrDefault(
                        category =>
                            category.Category.Equals(
                                cleanCategory,
                                StringComparison.OrdinalIgnoreCase));


            if (categoryBudget == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "The selected spending category does not exist in this monthly budget.",

                        category =
                            cleanCategory
                    });
            }


            // =================================================
            // IDEMPOTENCY CHECK
            //
            // Transactions belong directly to UserId.
            // =================================================

            var existingTransaction =
                await _context
                    .Transactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        transaction =>
                            transaction.UserId ==
                                userId.Value &&
                            transaction.RequestId ==
                                cleanRequestId);


            if (existingTransaction != null)
            {
                return Ok(
                    new
                    {
                        message =
                            "This purchase request has already been recorded.",

                        duplicate =
                            true,

                        transactionId =
                            existingTransaction.Id,

                        transactionReference =
                            existingTransaction.Reference,

                        requestId =
                            existingTransaction.RequestId,

                        category =
                            existingTransaction.Category,

                        amount =
                            Math.Abs(
                                existingTransaction.Amount),

                        createdAt =
                            existingTransaction.CreatedAt,

                        paymentProcessed =
                            false
                    });
            }


            // =================================================
            // BUDGET INFORMATION
            // =================================================

            decimal categoryAvailable =
                categoryBudget.LimitAmount -
                categoryBudget.SpentAmount;


            bool withinBudget =
                request.Amount <=
                categoryAvailable;


            decimal projectedSpent =
                categoryBudget.SpentAmount +
                request.Amount;


            decimal projectedRemaining =
                categoryBudget.LimitAmount -
                projectedSpent;


            // =================================================
            // DATABASE TRANSACTION
            // =================================================

            await using var databaseTransaction =
                await _context
                    .Database
                    .BeginTransactionAsync();


            try
            {
                // =============================================
                // UPDATE CATEGORY SPENDING
                // =============================================

                categoryBudget.SpentAmount +=
                    request.Amount;


                // =============================================
                // CREATE SPENDING HISTORY RECORD
                //
                // Negative amount represents spending.
                //
                // No money is moved by SpendWise.
                // =============================================

                var spendingRecord =
                    new Transaction
                    {
                        UserId =
                            userId.Value,

                        Amount =
                            -request.Amount,

                        Type =
                            "Purchase",

                        Category =
                            categoryBudget.Category,

                        RequestId =
                            cleanRequestId,

                        Description =
                            cleanDescription,

                        Reference =
                            $"PURCHASE-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",

                        CreatedAt =
                            DateTime.UtcNow
                    };


                _context
                    .Transactions
                    .Add(
                        spendingRecord);


                // =============================================
                // SAVE CHANGES
                // =============================================

                await _context
                    .SaveChangesAsync();


                await databaseTransaction
                    .CommitAsync();


                // =============================================
                // SUCCESS
                // =============================================

                return Ok(
                    new
                    {
                        message =
                            "Purchase recorded successfully. No payment was processed by SpendWise.",

                        duplicate =
                            false,

                        transactionId =
                            spendingRecord.Id,

                        requestId =
                            spendingRecord.RequestId,

                        category =
                            categoryBudget.Category,

                        description =
                            cleanDescription,

                        purchaseAmount =
                            request.Amount,

                        categoryBudget =
                            categoryBudget.LimitAmount,

                        categorySpent =
                            categoryBudget.SpentAmount,

                        categoryRemaining =
                            projectedRemaining,

                        withinBudget,

                        budgetStatus =
                            withinBudget
                                ? "Within budget"
                                : "Over budget",

                        transactionReference =
                            spendingRecord.Reference,

                        month =
                            request.Month,

                        year =
                            request.Year,

                        paymentProcessed =
                            false
                    });
            }


            // =================================================
            // CONCURRENCY / DUPLICATE PROTECTION
            // =================================================

            catch (DbUpdateConcurrencyException)
            {
                await databaseTransaction
                    .RollbackAsync();


                _context
                    .ChangeTracker
                    .Clear();


                var duplicateTransaction =
                    await _context
                        .Transactions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            transaction =>
                                transaction.UserId ==
                                    userId.Value &&
                                transaction.RequestId ==
                                    cleanRequestId);


                if (duplicateTransaction != null)
                {
                    return Ok(
                        new
                        {
                            message =
                                "This purchase request has already been recorded.",

                            duplicate =
                                true,

                            transactionId =
                                duplicateTransaction.Id,

                            transactionReference =
                                duplicateTransaction.Reference,

                            requestId =
                                duplicateTransaction.RequestId,

                            category =
                                duplicateTransaction.Category,

                            amount =
                                Math.Abs(
                                    duplicateTransaction.Amount),

                            createdAt =
                                duplicateTransaction.CreatedAt,

                            paymentProcessed =
                                false
                        });
                }


                return Conflict(
                    new
                    {
                        message =
                            "The spending information changed while this purchase was being recorded. Please try again.",

                        retryRequired =
                            true
                    });
            }


            // =================================================
            // DATABASE UPDATE ERROR
            // =================================================

            catch (DbUpdateException)
            {
                await databaseTransaction
                    .RollbackAsync();


                _context
                    .ChangeTracker
                    .Clear();


                var duplicateTransaction =
                    await _context
                        .Transactions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            transaction =>
                                transaction.UserId ==
                                    userId.Value &&
                                transaction.RequestId ==
                                    cleanRequestId);


                if (duplicateTransaction != null)
                {
                    return Ok(
                        new
                        {
                            message =
                                "This purchase request has already been recorded.",

                            duplicate =
                                true,

                            transactionId =
                                duplicateTransaction.Id,

                            transactionReference =
                                duplicateTransaction.Reference,

                            requestId =
                                duplicateTransaction.RequestId,

                            category =
                                duplicateTransaction.Category,

                            amount =
                                Math.Abs(
                                    duplicateTransaction.Amount),

                            createdAt =
                                duplicateTransaction.CreatedAt,

                            paymentProcessed =
                                false
                        });
                }


                return StatusCode(
                    StatusCodes
                        .Status500InternalServerError,

                    new
                    {
                        message =
                            "Recording the purchase failed. No changes were completed."
                    });
            }


            // =================================================
            // OTHER ERROR
            // =================================================

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
                            "Recording the purchase failed. No changes were completed."
                    });
            }
        }


        // =====================================================
        // GET PURCHASE RECORDS WITH FILTERS
        //
        // GET: /api/Purchase
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetPurchases(
            [FromQuery] string? category,
            [FromQuery] string? search,
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromQuery] decimal? minAmount,
            [FromQuery] decimal? maxAmount)
        {
            int? userId =
                GetCurrentUserId();


            if (userId == null)
            {
                return Unauthorized();
            }


            // =================================================
            // VALIDATE MONTH
            // =================================================

            if (month.HasValue &&
                (month.Value < 1 ||
                 month.Value > 12))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Month must be between 1 and 12."
                    });
            }


            // =================================================
            // VALIDATE YEAR
            // =================================================

            if (year.HasValue &&
                (year.Value < 2020 ||
                 year.Value > 2100))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Invalid year."
                    });
            }


            // =================================================
            // VALIDATE MINIMUM AMOUNT
            // =================================================

            if (minAmount.HasValue &&
                minAmount.Value < 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Minimum amount cannot be negative."
                    });
            }


            // =================================================
            // VALIDATE MAXIMUM AMOUNT
            // =================================================

            if (maxAmount.HasValue &&
                maxAmount.Value < 0)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Maximum amount cannot be negative."
                    });
            }


            // =================================================
            // VALIDATE RANGE
            // =================================================

            if (minAmount.HasValue &&
                maxAmount.HasValue &&
                minAmount.Value >
                    maxAmount.Value)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Minimum amount cannot be greater than maximum amount."
                    });
            }


            // =================================================
            // START PURCHASE QUERY
            //
            // Purchases belong directly to UserId.
            // =================================================

            var query =
                _context
                    .Transactions
                    .AsNoTracking()
                    .Where(
                        transaction =>
                            transaction.UserId ==
                                userId.Value &&
                            transaction.Type ==
                                "Purchase");


            // =================================================
            // CATEGORY FILTER
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                    category))
            {
                string cleanCategory =
                    category.Trim();


                query =
                    query.Where(
                        transaction =>
                            transaction.Category != null &&
                            transaction.Category ==
                                cleanCategory);
            }


            // =================================================
            // SEARCH FILTER
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                    search))
            {
                string cleanSearch =
                    search.Trim();


                query =
                    query.Where(
                        transaction =>
                            transaction.Description.Contains(
                                cleanSearch) ||

                            (transaction.Category != null &&
                             transaction.Category.Contains(
                                 cleanSearch)) ||

                            transaction.Reference.Contains(
                                cleanSearch));
            }


            // =================================================
            // MONTH FILTER
            // =================================================

            if (month.HasValue)
            {
                query =
                    query.Where(
                        transaction =>
                            transaction.CreatedAt.Month ==
                                month.Value);
            }


            // =================================================
            // YEAR FILTER
            // =================================================

            if (year.HasValue)
            {
                query =
                    query.Where(
                        transaction =>
                            transaction.CreatedAt.Year ==
                                year.Value);
            }


            // =================================================
            // MINIMUM AMOUNT FILTER
            // =================================================

            if (minAmount.HasValue)
            {
                query =
                    query.Where(
                        transaction =>
                            -transaction.Amount >=
                                minAmount.Value);
            }


            // =================================================
            // MAXIMUM AMOUNT FILTER
            // =================================================

            if (maxAmount.HasValue)
            {
                query =
                    query.Where(
                        transaction =>
                            -transaction.Amount <=
                                maxAmount.Value);
            }


            // =================================================
            // GET RESULTS
            // =================================================

            var purchases =
                await query
                    .OrderByDescending(
                        transaction =>
                            transaction.CreatedAt)
                    .Select(
                        transaction =>
                            new
                            {
                                id =
                                    transaction.Id,

                                category =
                                    transaction.Category,

                                amount =
                                    -transaction.Amount,

                                type =
                                    transaction.Type,

                                description =
                                    transaction.Description,

                                reference =
                                    transaction.Reference,

                                requestId =
                                    transaction.RequestId,

                                createdAt =
                                    transaction.CreatedAt
                            })
                    .ToListAsync();


            // =================================================
            // TOTAL PURCHASE VALUE
            // =================================================

            decimal totalAmount =
                purchases.Sum(
                    purchase =>
                        purchase.amount);


            // =================================================
            // RETURN FILTERED PURCHASES
            // =================================================

            return Ok(
                new
                {
                    count =
                        purchases.Count,

                    totalAmount,

                    filters =
                        new
                        {
                            category,
                            search,
                            month,
                            year,
                            minAmount,
                            maxAmount
                        },

                    purchases
                });
        }


        // =====================================================
        // GET CURRENT USER ID FROM JWT
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