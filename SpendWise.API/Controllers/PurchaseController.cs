
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

        public PurchaseController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================
        // POST: api/Purchase
        // ============================================

        [HttpPost]
        public async Task<IActionResult> CreatePurchase(
            [FromBody] CreatePurchaseDto request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Category))
                return BadRequest(new
                {
                    message = "Category is required."
                });

            if (string.IsNullOrWhiteSpace(request.Description))
                return BadRequest(new
                {
                    message = "Description is required."
                });

            if (request.Amount <= 0)
                return BadRequest(new
                {
                    message = "Purchase amount must be greater than R0."
                });

            if (request.Month < 1 || request.Month > 12)
                return BadRequest(new
                {
                    message = "Month must be between 1 and 12."
                });

            if (request.Year < 2020 || request.Year > 2100)
                return BadRequest(new
                {
                    message = "Invalid year."
                });

            if (string.IsNullOrWhiteSpace(request.RequestId))
                return BadRequest(new
                {
                    message = "RequestId is required."
                });

            string cleanRequestId = request.RequestId.Trim();
            string cleanCategory = request.Category.Trim();
            string cleanDescription = request.Description.Trim();

            var existingTransaction = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId.Value &&
                    t.RequestId == cleanRequestId);

            if (existingTransaction != null)
                return DuplicateResponse(existingTransaction);

            var monthlyBudget = await _context.MonthlyBudgets
                .Include(b => b.CategoryBudgets)
                .FirstOrDefaultAsync(b =>
                    b.UserId == userId.Value &&
                    b.Month == request.Month &&
                    b.Year == request.Year);

            if (monthlyBudget == null)
                return NotFound(new
                {
                    message =
                        "No monthly budget was found for the selected month and year."
                });

            var categoryBudget = monthlyBudget.CategoryBudgets
                .FirstOrDefault(c =>
                    c.Category.Equals(
                        cleanCategory,
                        StringComparison.OrdinalIgnoreCase));

            if (categoryBudget == null)
                return NotFound(new
                {
                    message =
                        "The selected spending category does not exist in this monthly budget.",
                    category = cleanCategory
                });

            decimal budgetLimit = categoryBudget.LimitAmount;
            decimal previousSpent = categoryBudget.SpentAmount;
            decimal projectedSpent = previousSpent + request.Amount;
            decimal projectedRemaining = budgetLimit - projectedSpent;

            decimal categoryAvailable = budgetLimit - previousSpent;
            bool withinBudget = request.Amount <= categoryAvailable;

            decimal previousPercentage = budgetLimit > 0
                ? previousSpent / budgetLimit * 100m
                : 0m;

            decimal newPercentage = budgetLimit > 0
                ? projectedSpent / budgetLimit * 100m
                : 0m;

            var pendingAlerts =
                new List<(int Threshold, string Message)>();

            if (budgetLimit == 0m)
            {
                if (previousSpent == 0m && projectedSpent > 0m)
                {
                    pendingAlerts.Add((
                        101,
                        $"Your {categoryBudget.Category} category has a R0 budget and is now over budget."
                    ));
                }
            }
            else
            {
                if (previousPercentage < 75m &&
                    newPercentage >= 75m)
                {
                    pendingAlerts.Add((
                        75,
                        $"You have reached 75% of your {categoryBudget.Category} budget."
                    ));
                }

                if (previousPercentage < 90m &&
                    newPercentage >= 90m)
                {
                    pendingAlerts.Add((
                        90,
                        $"You have reached 90% of your {categoryBudget.Category} budget."
                    ));
                }

                if (previousPercentage < 100m &&
                    newPercentage >= 100m)
                {
                    pendingAlerts.Add((
                        100,
                        $"You have reached 100% of your {categoryBudget.Category} budget."
                    ));
                }

                if (previousSpent <= budgetLimit &&
                    projectedSpent > budgetLimit)
                {
                    pendingAlerts.Add((
                        101,
                        $"You have exceeded your {categoryBudget.Category} budget."
                    ));
                }
            }

            await using var databaseTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var duplicate = await _context.Transactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.UserId == userId.Value &&
                        t.RequestId == cleanRequestId);

                if (duplicate != null)
                {
                    await databaseTransaction.RollbackAsync();
                    return DuplicateResponse(duplicate);
                }

                categoryBudget.SpentAmount = projectedSpent;

                var spendingRecord = new Transaction
                {
                    UserId = userId.Value,
                    Amount = -request.Amount,
                    Type = "Purchase",
                    Category = categoryBudget.Category,
                    RequestId = cleanRequestId,
                    Description = cleanDescription,
                    Reference =
                        $"PURCHASE-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(spendingRecord);

                var savedAlerts = new List<BudgetNotification>();

                foreach (var alert in pendingAlerts)
                {
                    bool alreadyExists = await _context
                        .BudgetNotifications
                        .AnyAsync(n =>
                            n.UserId == userId.Value &&
                            n.Category == categoryBudget.Category &&
                            n.Month == request.Month &&
                            n.Year == request.Year &&
                            n.Threshold == alert.Threshold);

                    if (alreadyExists)
                        continue;

                    var notification = new BudgetNotification
                    {
                        UserId = userId.Value,
                        Category = categoryBudget.Category,
                        Threshold = alert.Threshold,
                        Message = alert.Message,
                        Month = request.Month,
                        Year = request.Year,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.BudgetNotifications.Add(notification);
                    savedAlerts.Add(notification);
                }

                await _context.SaveChangesAsync();
                await databaseTransaction.CommitAsync();

                return Ok(new
                {
                    message =
                        "Purchase recorded successfully. No payment was processed by SpendWise.",
                    duplicate = false,
                    transactionId = spendingRecord.Id,
                    requestId = spendingRecord.RequestId,
                    category = categoryBudget.Category,
                    description = cleanDescription,
                    purchaseAmount = request.Amount,
                    categoryBudget = budgetLimit,
                    categorySpent = categoryBudget.SpentAmount,
                    categoryRemaining = projectedRemaining,
                    withinBudget,
                    budgetPercentageUsed = budgetLimit > 0m
                        ? (decimal?)newPercentage
                        : null,
                    budgetAlerts = savedAlerts
                        .Select(n => n.Message)
                        .ToList(),
                    notificationsCreated = savedAlerts.Count,
                    budgetStatus = withinBudget
                        ? "Within budget"
                        : "Over budget",
                    transactionReference = spendingRecord.Reference,
                    month = request.Month,
                    year = request.Year,
                    paymentProcessed = false
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                await databaseTransaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                var duplicate = await FindDuplicateAsync(
                    userId.Value, cleanRequestId);

                if (duplicate != null)
                    return DuplicateResponse(duplicate);

                return Conflict(new
                {
                    message =
                        "The spending information changed while this purchase was being recorded. Please try again.",
                    retryRequired = true
                });
            }
            catch (DbUpdateException)
            {
                await databaseTransaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                var duplicate = await FindDuplicateAsync(
                    userId.Value, cleanRequestId);

                if (duplicate != null)
                    return DuplicateResponse(duplicate);

                return StatusCode(500, new
                {
                    message =
                        "Recording the purchase failed. No changes were completed."
                });
            }
            catch
            {
                await databaseTransaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                return StatusCode(500, new
                {
                    message =
                        "Recording the purchase failed. No changes were completed."
                });
            }
        }

        // ============================================
        // GET: api/Purchase
        // ============================================

        [HttpGet]
        public async Task<IActionResult> GetPurchases(
            [FromQuery] string? category,
            [FromQuery] string? search,
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromQuery] decimal? minAmount,
            [FromQuery] decimal? maxAmount)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (month.HasValue &&
                (month.Value < 1 || month.Value > 12))
                return BadRequest(new
                {
                    message = "Month must be between 1 and 12."
                });

            if (year.HasValue &&
                (year.Value < 2020 || year.Value > 2100))
                return BadRequest(new
                {
                    message = "Invalid year."
                });

            if (minAmount.HasValue && minAmount.Value < 0)
                return BadRequest(new
                {
                    message = "Minimum amount cannot be negative."
                });

            if (maxAmount.HasValue && maxAmount.Value < 0)
                return BadRequest(new
                {
                    message = "Maximum amount cannot be negative."
                });

            if (minAmount.HasValue &&
                maxAmount.HasValue &&
                minAmount.Value > maxAmount.Value)
                return BadRequest(new
                {
                    message =
                        "Minimum amount cannot be greater than maximum amount."
                });

            var query = _context.Transactions
                .AsNoTracking()
                .Where(t =>
                    t.UserId == userId.Value &&
                    t.Type == "Purchase");

            if (!string.IsNullOrWhiteSpace(category))
            {
                string cleanCategory = category.Trim();

                query = query.Where(t =>
                    t.Category != null &&
                    t.Category == cleanCategory);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string cleanSearch = search.Trim();

                query = query.Where(t =>
                    t.Description.Contains(cleanSearch) ||
                    (t.Category != null &&
                     t.Category.Contains(cleanSearch)) ||
                    t.Reference.Contains(cleanSearch));
            }

            if (month.HasValue)
            {
                query = query.Where(t =>
                    t.CreatedAt.Month == month.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(t =>
                    t.CreatedAt.Year == year.Value);
            }

            if (minAmount.HasValue)
            {
                query = query.Where(t =>
                    -t.Amount >= minAmount.Value);
            }

            if (maxAmount.HasValue)
            {
                query = query.Where(t =>
                    -t.Amount <= maxAmount.Value);
            }

            var purchases = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    id = t.Id,
                    category = t.Category,
                    amount = -t.Amount,
                    type = t.Type,
                    description = t.Description,
                    reference = t.Reference,
                    requestId = t.RequestId,
                    createdAt = t.CreatedAt
                })
                .ToListAsync();

            decimal totalAmount = purchases.Sum(p => p.amount);

            return Ok(new
            {
                count = purchases.Count,
                totalAmount,
                filters = new
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

        // ============================================
        // DELETE: api/Purchase/{id}
        // ============================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePurchase(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (id <= 0)
                return BadRequest(new
                {
                    message = "Invalid purchase ID."
                });

            await using var databaseTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var purchase = await _context.Transactions
                    .FirstOrDefaultAsync(t =>
                        t.Id == id &&
                        t.UserId == userId.Value &&
                        t.Type == "Purchase");

                if (purchase == null)
                    return NotFound(new
                    {
                        message = "Purchase not found."
                    });

                if (purchase.Amount >= 0)
                    return Conflict(new
                    {
                        message =
                            "This record is not a valid spending purchase."
                    });

                if (string.IsNullOrWhiteSpace(purchase.Category))
                    return Conflict(new
                    {
                        message = "Purchase category is missing."
                    });

                decimal purchaseAmount = -purchase.Amount;

                int month = purchase.CreatedAt.Month;
                int year = purchase.CreatedAt.Year;

                var monthlyBudget = await _context.MonthlyBudgets
                    .Include(b => b.CategoryBudgets)
                    .FirstOrDefaultAsync(b =>
                        b.UserId == userId.Value &&
                        b.Month == month &&
                        b.Year == year);

                if (monthlyBudget == null)
                    return Conflict(new
                    {
                        message =
                            "The matching monthly budget could not be found. Purchase was not deleted."
                    });

                var categoryBudget = monthlyBudget.CategoryBudgets
                    .FirstOrDefault(c =>
                        c.Category.Equals(
                            purchase.Category,
                            StringComparison.OrdinalIgnoreCase));

                if (categoryBudget == null)
                    return Conflict(new
                    {
                        message =
                            "The matching category budget could not be found. Purchase was not deleted."
                    });

                if (categoryBudget.SpentAmount < purchaseAmount)
                    return Conflict(new
                    {
                        message =
                            "The category spending total is inconsistent. Purchase was not deleted."
                    });

                categoryBudget.SpentAmount -= purchaseAmount;

                _context.Transactions.Remove(purchase);

                await _context.SaveChangesAsync();
                await databaseTransaction.CommitAsync();

                return Ok(new
                {
                    message = "Purchase deleted successfully.",
                    deletedTransactionId = id,
                    category = categoryBudget.Category,
                    amountRemoved = purchaseAmount,
                    categorySpent = categoryBudget.SpentAmount,
                    categoryRemaining =
                        categoryBudget.LimitAmount -
                        categoryBudget.SpentAmount
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                await databaseTransaction.RollbackAsync();

                return Conflict(new
                {
                    message =
                        "Spending changed during deletion. Refresh and try again."
                });
            }
            catch
            {
                await databaseTransaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message =
                        "Deletion failed. No changes were completed."
                });
            }
        }

        // ============================================
        // DUPLICATE PURCHASE LOOKUP
        // ============================================

        private async Task<Transaction?> FindDuplicateAsync(
            int userId,
            string requestId)
        {
            return await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    t.RequestId == requestId);
        }

        // ============================================
        // DUPLICATE PURCHASE RESPONSE
        // ============================================

        private IActionResult DuplicateResponse(
            Transaction transaction)
        {
            return Ok(new
            {
                message =
                    "This purchase request has already been recorded.",
                duplicate = true,
                transactionId = transaction.Id,
                transactionReference = transaction.Reference,
                requestId = transaction.RequestId,
                category = transaction.Category,
                amount = Math.Abs(transaction.Amount),
                createdAt = transaction.CreatedAt,
                paymentProcessed = false
            });
        }

        // ============================================
        // CURRENT USER ID FROM JWT
        // ============================================

        private int? GetCurrentUserId()
        {
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out int userId))
                return null;

            return userId;
        }
    }
}
