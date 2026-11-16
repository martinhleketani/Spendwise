
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.Models;

namespace SpendWise.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(
        AuthenticationSchemes = "AdminCookie",
        Roles = "Administrator")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _context;

        public AdminDashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dashboard = new AdminDashboardViewModel
            {
                RegisteredUsers = await _context.Users.CountAsync(),

                ActiveUsers = await _context.Users
                    .CountAsync(u => u.IsActive),

                TotalTransactions = await _context.Transactions
                    .CountAsync(),

                TotalTransactionValue = await _context.Transactions
                    .Select(t => (decimal?)t.Amount)
                    .SumAsync() ?? 0m,

                MonthlyBudgets = await _context.MonthlyBudgets
                    .CountAsync(),

                TotalBudgetAllocation = await _context.MonthlyBudgets
                    .Select(b => (decimal?)b.TotalLimit)
                    .SumAsync() ?? 0m,

                GroceryLists = await _context.GroceryLists
                    .CountAsync(),

                ActiveGroceryLists = await _context.GroceryLists
                    .CountAsync(g => g.Status == "Active"),

                TotalNotifications = await _context.BudgetNotifications
                    .CountAsync(),

                UnreadNotifications = await _context.BudgetNotifications
                    .CountAsync(n => !n.IsRead)
            };

            // Retrieve recent customer registrations.
            var registrations = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => new AdminActivityViewModel
                {
                    Title = "New customer registered",
                    Description = u.FullName + " registered an account",
                    CreatedAt = u.CreatedAt,
                    Icon = "♙",
                    ActivityType = "Registration"
                })
                .ToListAsync();

            // Retrieve recent transactions.
            var transactions = await _context.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new AdminActivityViewModel
                {
                    Title = "Transaction recorded",
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    Icon = "⇄",
                    ActivityType = "Transaction"
                })
                .ToListAsync();

            // Retrieve recent grocery lists.
            var groceryLists = await _context.GroceryLists
                .OrderByDescending(g => g.CreatedAt)
                .Take(5)
                .Select(g => new AdminActivityViewModel
                {
                    Title = "Grocery list created",
                    Description = g.ListName + " - " + g.Retailer,
                    CreatedAt = g.CreatedAt,
                    Icon = "▤",
                    ActivityType = "Grocery List"
                })
                .ToListAsync();

            // Retrieve recent budget notifications.
            var notifications = await _context.BudgetNotifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new AdminActivityViewModel
                {
                    Title = "Budget notification generated",
                    Description = n.Message,
                    CreatedAt = n.CreatedAt,
                    Icon = "!",
                    ActivityType = "Notification"
                })
                .ToListAsync();

            dashboard.RecentActivity = registrations
                .Concat(transactions)
                .Concat(groceryLists)
                .Concat(notifications)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToList();

            // These statuses reflect what this request has actually verified.
            dashboard.DatabaseStatus = "Connected";
            dashboard.AdminPortalStatus = "Authorized";
            dashboard.CustomerApiStatus = "Not independently tested";
            dashboard.PaymentStatus = "Not configured";

            return View(dashboard);
        }
    }

    public class AdminDashboardViewModel
    {
        public int RegisteredUsers { get; set; }

        public int ActiveUsers { get; set; }

        public int TotalTransactions { get; set; }

        public decimal TotalTransactionValue { get; set; }

        public int MonthlyBudgets { get; set; }

        public decimal TotalBudgetAllocation { get; set; }

        public int GroceryLists { get; set; }

        public int ActiveGroceryLists { get; set; }

        public int TotalNotifications { get; set; }

        public int UnreadNotifications { get; set; }

        public string DatabaseStatus { get; set; } = "Not checked";

        public string AdminPortalStatus { get; set; } = "Not checked";

        public string CustomerApiStatus { get; set; } = "Not checked";

        public string PaymentStatus { get; set; } = "Not configured";

        public List<AdminActivityViewModel> RecentActivity { get; set; }
            = new();
    }

    public class AdminActivityViewModel
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string Icon { get; set; } = string.Empty;

        public string ActivityType { get; set; } = string.Empty;
    }
}
