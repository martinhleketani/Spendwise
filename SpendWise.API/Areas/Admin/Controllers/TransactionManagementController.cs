
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.Services;

namespace SpendWise.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(
        AuthenticationSchemes = "AdminCookie",
        Roles = "Administrator")]
    public class TransactionManagementController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;

        private static readonly List<string> AvailableCategories = new()
        {
            "Groceries",
            "Entertainment",
            "Other",
            "Transport",
            "Utilities"
        };

        public TransactionManagementController(
            AppDbContext context,
            IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // ==========================================
        // TRANSACTION DIRECTORY
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? type,
            string? category)
        {
            var query = _context.Transactions
                .AsNoTracking()
                .Include(t => t.User)
                .AsQueryable();

            // SEARCH TRANSACTIONS

            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchTerm = search.Trim();

                query = query.Where(t =>
                    t.Reference.Contains(searchTerm) ||
                    t.Description.Contains(searchTerm) ||
                    t.User.FullName.Contains(searchTerm) ||
                    t.User.Email.Contains(searchTerm));
            }

            // FILTER BY TRANSACTION TYPE

            if (!string.IsNullOrWhiteSpace(type))
            {
                string selectedType = type.Trim();

                query = query.Where(t =>
                    t.Type == selectedType);
            }

            // FILTER BY CATEGORY

            if (!string.IsNullOrWhiteSpace(category))
            {
                string selectedCategory = category.Trim();

                if (selectedCategory == "Groceries" ||
                    selectedCategory == "Grocery")
                {
                    query = query.Where(t =>
                        t.Category == "Groceries" ||
                        t.Category == "Grocery");
                }
                else
                {
                    query = query.Where(t =>
                        t.Category == selectedCategory);
                }
            }

            // LOAD TRANSACTIONS

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TransactionListItem
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    CustomerName = t.User.FullName,
                    CustomerEmail = t.User.Email,
                    Amount = t.Amount,
                    Type = t.Type,
                    Category = t.Category,
                    Retailer = t.Retailer,
                    RequestId = t.RequestId,
                    Description = t.Description,
                    Reference = t.Reference,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            // LOAD TRANSACTION TYPES FROM DATABASE

            var types = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Type != null && t.Type != "")
                .Select(t => t.Type)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            // USE ALL FIVE AVAILABLE CATEGORIES

            var categories = AvailableCategories;

            // DASHBOARD STATISTICS

            var model = new TransactionManagementIndexViewModel
            {
                Transactions = transactions,

                Search = search ?? string.Empty,

                SelectedType = type ?? string.Empty,

                SelectedCategory = category ?? string.Empty,

                Types = types,

                Categories = categories,

                TotalTransactions =
                    await _context.Transactions.CountAsync(),

                TotalTransactionValue =
                    await _context.Transactions
                        .Select(t => (decimal?)t.Amount)
                        .SumAsync() ?? 0m
            };

            return View(model);
        }

        // ==========================================
        // TRANSACTION DETAILS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TransactionListItem
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    CustomerName = t.User.FullName,
                    CustomerEmail = t.User.Email,
                    Amount = t.Amount,
                    Type = t.Type,
                    Category = t.Category,
                    Retailer = t.Retailer,
                    RequestId = t.RequestId,
                    Description = t.Description,
                    Reference = t.Reference,
                    CreatedAt = t.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (transaction == null)
            {
                return NotFound();
            }

            // RECORD TRANSACTION DETAILS ACCESS

            await _auditLogService.LogAsync(
                action: "Viewed",
                module: "Transactions",
                description:
                    $"Administrator viewed transaction {transaction.Reference} belonging to customer {transaction.CustomerName}.",
                entityType: "Transaction",
                entityId: transaction.Id.ToString());

            return View(transaction);
        }
    }

    // ==========================================
    // TRANSACTION LIST ITEM
    // ==========================================

    public class TransactionListItem
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Type { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? Retailer { get; set; }

        public string? RequestId { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    // ==========================================
    // TRANSACTION MANAGEMENT VIEW MODEL
    // ==========================================

    public class TransactionManagementIndexViewModel
    {
        public List<TransactionListItem> Transactions { get; set; }
            = new();

        public string Search { get; set; } = string.Empty;

        public string SelectedType { get; set; } = string.Empty;

        public string SelectedCategory { get; set; } = string.Empty;

        public List<string> Types { get; set; } = new();

        public List<string> Categories { get; set; } = new();

        public int TotalTransactions { get; set; }

        public decimal TotalTransactionValue { get; set; }
    }
}
