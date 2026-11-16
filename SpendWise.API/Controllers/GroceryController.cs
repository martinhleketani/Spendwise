
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
    public class GroceryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GroceryController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // CREATE GROCERY LIST
        // POST: /api/Grocery
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> CreateList(
            CreateGroceryListDto request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.ListName))
            {
                return BadRequest(new
                {
                    message = "List name is required."
                });
            }

            var groceryList = new GroceryList
            {
                UserId = userId.Value,
                ListName = request.ListName.Trim(),

                // Retained temporarily for compatibility.
                Retailer = string.IsNullOrWhiteSpace(request.Retailer)
                    ? "Other"
                    : request.Retailer.Trim(),

                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.GroceryLists.Add(groceryList);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Grocery list created successfully.",
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
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var lists = await _context.GroceryLists
                .Where(g => g.UserId == userId.Value)
                .Include(g => g.Items)
                .OrderByDescending(g => g.CreatedAt)
                .Select(g => new
                {
                    g.Id,
                    g.ListName,
                    g.Retailer,
                    g.Status,
                    ItemCount = g.Items.Count,
                    Total = g.Items.Sum(i =>
                        i.UnitPrice * i.Quantity),
                    g.CreatedAt,
                    g.UpdatedAt
                })
                .ToListAsync();

            return Ok(lists);
        }

        // =====================================================
        // GET ONE GROCERY LIST
        // GET: /api/Grocery/{id}
        // =====================================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetList(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var groceryList = await _context.GroceryLists
                .Include(g => g.Items)
                .FirstOrDefaultAsync(g =>
                    g.Id == id &&
                    g.UserId == userId.Value);

            if (groceryList == null)
            {
                return NotFound(new
                {
                    message = "Grocery list not found."
                });
            }

            decimal total = groceryList.Items.Sum(i =>
                i.UnitPrice * i.Quantity);

            return Ok(new
            {
                groceryList.Id,
                groceryList.ListName,
                groceryList.Retailer,
                groceryList.Status,
                ItemCount = groceryList.Items.Count,
                Total = total,

                Items = groceryList.Items.Select(i => new
                {
                    i.Id,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice,
                    TotalPrice = i.TotalPrice,
                    i.IsPurchased,
                    i.CreatedAt
                }),

                groceryList.CreatedAt,
                groceryList.UpdatedAt
            });
        }

        // =====================================================
        // ADD MANUAL GROCERY ITEM
        // POST: /api/Grocery/{id}/items
        // =====================================================

        [HttpPost("{id:int}/items")]
        public async Task<IActionResult> AddItem(
            int id,
            AddGroceryItemDto request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var groceryList = await _context.GroceryLists
                .FirstOrDefaultAsync(g =>
                    g.Id == id &&
                    g.UserId == userId.Value);

            if (groceryList == null)
            {
                return NotFound(new
                {
                    message = "Grocery list not found."
                });
            }

            if (!groceryList.Status.Equals(
                "Active",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Only active grocery lists can be edited."
                });
            }

            if (string.IsNullOrWhiteSpace(request.ProductName))
            {
                return BadRequest(new
                {
                    message = "Product name is required."
                });
            }

            if (request.Quantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Quantity must be greater than zero."
                });
            }

            if (request.UnitPrice <= 0)
            {
                return BadRequest(new
                {
                    message = "Unit price must be greater than zero."
                });
            }

            var item = new GroceryItem
            {
                GroceryListId = groceryList.Id,
                ProductName = request.ProductName.Trim(),
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                IsPurchased = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.GroceryItems.Add(item);

            groceryList.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            decimal lineTotal = item.TotalPrice;

            decimal listTotal = await _context.GroceryItems
                .Where(i => i.GroceryListId == id)
                .SumAsync(i => i.UnitPrice * i.Quantity);

            return Ok(new
            {
                message = "Grocery item added successfully.",

                Item = new
                {
                    item.Id,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    TotalPrice = lineTotal
                },

                ListTotal = listTotal
            });
        }

        // =====================================================
        // UPDATE GROCERY ITEM
        // PUT: /api/Grocery/{listId}/items/{itemId}
        // =====================================================

        [HttpPut("{listId:int}/items/{itemId:int}")]
        public async Task<IActionResult> UpdateItem(
            int listId,
            int itemId,
            AddGroceryItemDto request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var groceryList = await _context.GroceryLists
                .FirstOrDefaultAsync(g =>
                    g.Id == listId &&
                    g.UserId == userId.Value);

            if (groceryList == null)
                return NotFound(new { message = "Grocery list not found." });

            if (!groceryList.Status.Equals(
                "Active",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Completed grocery lists cannot be edited."
                });
            }

            var item = await _context.GroceryItems
                .FirstOrDefaultAsync(i =>
                    i.Id == itemId &&
                    i.GroceryListId == listId);

            if (item == null)
                return NotFound(new { message = "Grocery item not found." });

            if (string.IsNullOrWhiteSpace(request.ProductName))
                return BadRequest(new { message = "Product name is required." });

            if (request.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than zero." });

            if (request.UnitPrice <= 0)
                return BadRequest(new { message = "Unit price must be greater than zero." });

            item.ProductName = request.ProductName.Trim();
            item.Quantity = request.Quantity;
            item.UnitPrice = request.UnitPrice;

            groceryList.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            decimal listTotal = await _context.GroceryItems
                .Where(i => i.GroceryListId == listId)
                .SumAsync(i => i.UnitPrice * i.Quantity);

            return Ok(new
            {
                message = "Grocery item updated successfully.",
                item.Id,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                TotalPrice = item.TotalPrice,
                ListTotal = listTotal
            });
        }

        // =====================================================
        // CHECK GROCERY LIST AGAINST MONTHLY BUDGET
        // POST: /api/Grocery/{id}/budget-check
        // =====================================================

        [HttpPost("{id:int}/budget-check")]
        public async Task<IActionResult> CheckBudget(
            int id,
            GroceryBudgetCheckDto request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (request.Month < 1 || request.Month > 12)
                return BadRequest(new { message = "Invalid month." });

            if (request.Year < 2020 || request.Year > 2100)
                return BadRequest(new { message = "Invalid year." });

            var groceryList = await _context.GroceryLists
                .Include(g => g.Items)
                .FirstOrDefaultAsync(g =>
                    g.Id == id &&
                    g.UserId == userId.Value);

            if (groceryList == null)
                return NotFound(new { message = "Grocery list not found." });

            var monthlyBudget = await _context.MonthlyBudgets
                .Include(b => b.CategoryBudgets)
                .FirstOrDefaultAsync(b =>
                    b.UserId == userId.Value &&
                    b.Month == request.Month &&
                    b.Year == request.Year);

            if (monthlyBudget == null)
                return NotFound(new
                {
                    message = "No monthly budget was found."
                });

            var groceryBudget = monthlyBudget.CategoryBudgets
                .FirstOrDefault(c =>
                    c.Category.Equals(
                        "Groceries",
                        StringComparison.OrdinalIgnoreCase));

            if (groceryBudget == null)
                return NotFound(new
                {
                    message = "No Groceries category exists in this budget."
                });

            decimal listTotal = groceryList.Items.Sum(i => i.TotalPrice);

            decimal availableBeforeList =
                groceryBudget.LimitAmount - groceryBudget.SpentAmount;

            decimal remainingAfterList =
                availableBeforeList - listTotal;

            bool withinBudget = remainingAfterList >= 0;

            return Ok(new
            {
                GroceryListId = groceryList.Id,
                GroceryListName = groceryList.ListName,
                Month = request.Month,
                Year = request.Year,
                GroceryBudgetLimit = groceryBudget.LimitAmount,
                AlreadySpent = groceryBudget.SpentAmount,
                AvailableBeforeList = availableBeforeList,
                ListTotal = listTotal,
                RemainingAfterList = remainingAfterList,
                WithinBudget = withinBudget,
                OverBudgetBy = Math.Max(0, -remainingAfterList),
                Status = withinBudget ? "Within budget" : "Over budget"
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
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (request.Month < 1 || request.Month > 12)
                return BadRequest(new { message = "Invalid month." });

            if (request.Year < 2020 || request.Year > 2100)
                return BadRequest(new { message = "Invalid year." });

            var groceryList = await _context.GroceryLists
                .Include(g => g.Items)
                .FirstOrDefaultAsync(g =>
                    g.Id == id &&
                    g.UserId == userId.Value);

            if (groceryList == null)
                return NotFound(new { message = "Grocery list not found." });

            if (!groceryList.Status.Equals(
                "Active",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "This grocery list is not active."
                });
            }

            if (!groceryList.Items.Any())
                return BadRequest(new
                {
                    message = "Cannot record an empty grocery list."
                });

            decimal listTotal = groceryList.Items.Sum(i => i.TotalPrice);

            if (listTotal <= 0)
                return BadRequest(new
                {
                    message = "The grocery list total must be greater than zero."
                });

            var monthlyBudget = await _context.MonthlyBudgets
                .Include(b => b.CategoryBudgets)
                .FirstOrDefaultAsync(b =>
                    b.UserId == userId.Value &&
                    b.Month == request.Month &&
                    b.Year == request.Year);

            if (monthlyBudget == null)
                return NotFound(new
                {
                    message = "No monthly budget was found."
                });

            var groceryBudget = monthlyBudget.CategoryBudgets
                .FirstOrDefault(c =>
                    c.Category.Equals(
                        "Groceries",
                        StringComparison.OrdinalIgnoreCase));

            if (groceryBudget == null)
                return NotFound(new
                {
                    message = "No Groceries category exists in this budget."
                });

            decimal available = groceryBudget.LimitAmount -
                                groceryBudget.SpentAmount;

            bool withinBudget = listTotal <= available;

            await using var databaseTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                groceryBudget.SpentAmount += listTotal;

                foreach (var item in groceryList.Items)
                    item.IsPurchased = true;

                groceryList.Status = "Completed";
                groceryList.UpdatedAt = DateTime.UtcNow;

                var spendingRecord = new Transaction
                {
                    UserId = userId.Value,
                    Amount = -listTotal,
                    Type = "Purchase",
                    Category = "Groceries",
                    Retailer = groceryList.Retailer,
                    Description = $"Grocery purchase: {groceryList.ListName}",
                    Reference = $"GROCERY-{groceryList.Id}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(spendingRecord);

                await _context.SaveChangesAsync();
                await databaseTransaction.CommitAsync();

                return Ok(new
                {
                    message = "Grocery purchase recorded successfully. No payment was processed.",
                    GroceryListId = groceryList.Id,
                    GroceryListName = groceryList.ListName,
                    PurchaseTotal = listTotal,
                    GroceryBudgetLimit = groceryBudget.LimitAmount,
                    GrocerySpent = groceryBudget.SpentAmount,
                    GroceryBudgetRemaining =
                        groceryBudget.LimitAmount - groceryBudget.SpentAmount,
                    WithinBudget = withinBudget,
                    TransactionReference = spendingRecord.Reference,
                    Status = groceryList.Status,
                    PaymentProcessed = false
                });
            }
            catch
            {
                await databaseTransaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "Recording the grocery purchase failed."
                    });
            }
        }

        // =====================================================
        // DELETE GROCERY ITEM
        // DELETE: /api/Grocery/{listId}/items/{itemId}
        // =====================================================

        [HttpDelete("{listId:int}/items/{itemId:int}")]
        public async Task<IActionResult> DeleteItem(
            int listId,
            int itemId)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var groceryList = await _context.GroceryLists
                .FirstOrDefaultAsync(g =>
                    g.Id == listId &&
                    g.UserId == userId.Value);

            if (groceryList == null)
                return NotFound(new { message = "Grocery list not found." });

            if (!groceryList.Status.Equals(
                "Active",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Items cannot be removed from completed lists."
                });
            }

            var item = await _context.GroceryItems
                .FirstOrDefaultAsync(i =>
                    i.Id == itemId &&
                    i.GroceryListId == listId);

            if (item == null)
                return NotFound(new { message = "Grocery item not found." });

            _context.GroceryItems.Remove(item);

            groceryList.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Grocery item deleted successfully." });
        }

        // =====================================================
        // DELETE GROCERY LIST
        // DELETE: /api/Grocery/{id}
        // =====================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteList(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var groceryList = await _context.GroceryLists
                .FirstOrDefaultAsync(g =>
                    g.Id == id &&
                    g.UserId == userId.Value);

            if (groceryList == null)
                return NotFound(new { message = "Grocery list not found." });

            if (!groceryList.Status.Equals(
                "Active",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Completed grocery lists cannot be deleted."
                });
            }

            _context.GroceryLists.Remove(groceryList);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Grocery list deleted successfully." });
        }

        // =====================================================
        // GET CURRENT LOGGED-IN USER
        // =====================================================

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
