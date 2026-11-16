
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.Models;
using SpendWise.API.Services;

namespace SpendWise.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(
        AuthenticationSchemes = "AdminCookie",
        Roles = "Administrator")]
    public class WalletManagementController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public WalletManagementController(
            AppDbContext context,
            IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // ==========================================
        // WALLET MANAGEMENT DASHBOARD
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var walletsQuery = _context.Wallets
                .AsNoTracking()
                .Include(w => w.User)
                .Include(w => w.Transactions)
                .AsQueryable();

            // Search wallets.
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                walletsQuery = walletsQuery.Where(w =>
                    w.WalletNumber.Contains(search) ||
                    w.User.FullName.Contains(search) ||
                    w.User.Email.Contains(search));
            }

            var wallets = await walletsQuery
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var walletViewModels = wallets.Select(wallet =>
            {
                var completedTransactions = wallet.Transactions
                    .Where(t => t.Status == "Completed");

                // Credits.
                decimal deposits = completedTransactions
                    .Where(t => t.Type == "Deposit")
                    .Sum(t => t.Amount);

                decimal refunds = completedTransactions
                    .Where(t => t.Type == "Refund")
                    .Sum(t => t.Amount);

                // Debits.
                decimal payments = completedTransactions
                    .Where(t => t.Type == "Payment")
                    .Sum(t => t.Amount);

                // Adjustments are excluded until their debit/credit
                // direction is explicitly defined.
                decimal balance = deposits + refunds - payments;

                return new WalletManagementItemViewModel
                {
                    WalletId = wallet.Id,
                    UserId = wallet.UserId,
                    CustomerName = wallet.User.FullName,
                    CustomerEmail = wallet.User.Email,
                    WalletNumber = wallet.WalletNumber,
                    Currency = wallet.Currency,
                    Status = wallet.Status,
                    Balance = balance,
                    TransactionCount = wallet.Transactions.Count,
                    CreatedAt = wallet.CreatedAt
                };
            }).ToList();

            var viewModel = new WalletManagementIndexViewModel
            {
                Search = search,
                Wallets = walletViewModels,
                TotalWallets = wallets.Count,
                ActiveWallets = wallets.Count(w => w.Status == "Active"),
                SuspendedWallets = wallets.Count(w => w.Status == "Suspended"),
                TotalCalculatedBalance = walletViewModels
                    .Where(w => w.Status == "Active")
                    .Sum(w => w.Balance)
            };

            return View(viewModel);
        }

        // ==========================================
        // WALLET DETAILS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var wallet = await _context.Wallets
                .AsNoTracking()
                .Include(w => w.User)
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (wallet == null)
            {
                return NotFound();
            }

            var completedTransactions = wallet.Transactions
                .Where(t => t.Status == "Completed");

            decimal deposits = completedTransactions
                .Where(t => t.Type == "Deposit")
                .Sum(t => t.Amount);

            decimal refunds = completedTransactions
                .Where(t => t.Type == "Refund")
                .Sum(t => t.Amount);

            decimal payments = completedTransactions
                .Where(t => t.Type == "Payment")
                .Sum(t => t.Amount);

            var viewModel = new WalletManagementDetailsViewModel
            {
                WalletId = wallet.Id,
                UserId = wallet.UserId,
                CustomerName = wallet.User.FullName,
                CustomerEmail = wallet.User.Email,
                WalletNumber = wallet.WalletNumber,
                Currency = wallet.Currency,
                Status = wallet.Status,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt,

                TotalDeposits = deposits,
                TotalRefunds = refunds,
                TotalPayments = payments,
                CalculatedBalance = deposits + refunds - payments,

                Transactions = wallet.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new WalletTransactionItemViewModel
                    {
                        Id = t.Id,
                        Type = t.Type,
                        Amount = t.Amount,
                        Category = t.Category,
                        Description = t.Description,
                        Reference = t.Reference,
                        ExternalReference = t.ExternalReference,
                        Status = t.Status,
                        Currency = t.Currency,
                        CreatedAt = t.CreatedAt,
                        CompletedAt = t.CompletedAt
                    })
                    .ToList()
            };

            // RECORD WALLET DETAILS ACCESS

            await _auditLogService.LogAsync(
                action: "Viewed",
                module: "Wallets",
                description:
                    $"Administrator viewed wallet {wallet.WalletNumber} belonging to customer {wallet.User.FullName}.",
                entityType: "Wallet",
                entityId: wallet.Id.ToString());

            return View(viewModel);
        }

        // ==========================================
        // SUSPEND WALLET
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.Id == id);

            if (wallet == null)
            {
                return NotFound();
            }

            if (wallet.Status == "Closed")
            {
                TempData["Error"] =
                    "A closed wallet cannot be suspended.";

                return RedirectToAction(nameof(Index));
            }

            wallet.Status = "Suspended";
            wallet.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // RECORD SUCCESSFUL SUSPENSION

            await _auditLogService.LogAsync(
                action: "Suspended",
                module: "Wallets",
                description:
                    $"Administrator suspended wallet {wallet.WalletNumber}.",
                entityType: "Wallet",
                entityId: wallet.Id.ToString());

            TempData["Success"] = "Wallet suspended successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // REACTIVATE WALLET
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.Id == id);

            if (wallet == null)
            {
                return NotFound();
            }

            if (wallet.Status != "Suspended")
            {
                TempData["Error"] =
                    "Only suspended wallets can be reactivated.";

                return RedirectToAction(nameof(Index));
            }

            wallet.Status = "Active";
            wallet.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // RECORD SUCCESSFUL REACTIVATION

            await _auditLogService.LogAsync(
                action: "Reactivated",
                module: "Wallets",
                description:
                    $"Administrator reactivated wallet {wallet.WalletNumber}.",
                entityType: "Wallet",
                entityId: wallet.Id.ToString());

            TempData["Success"] = "Wallet reactivated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }

    // ==========================================
    // WALLET MANAGEMENT VIEW MODELS
    // ==========================================

    public class WalletManagementIndexViewModel
    {
        public string? Search { get; set; }

        public int TotalWallets { get; set; }

        public int ActiveWallets { get; set; }

        public int SuspendedWallets { get; set; }

        public decimal TotalCalculatedBalance { get; set; }

        public List<WalletManagementItemViewModel> Wallets { get; set; }
            = new List<WalletManagementItemViewModel>();
    }

    public class WalletManagementItemViewModel
    {
        public int WalletId { get; set; }

        public int UserId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string WalletNumber { get; set; } = string.Empty;

        public string Currency { get; set; } = "ZAR";

        public string Status { get; set; } = string.Empty;

        public decimal Balance { get; set; }

        public int TransactionCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class WalletManagementDetailsViewModel
    {
        public int WalletId { get; set; }

        public int UserId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string WalletNumber { get; set; } = string.Empty;

        public string Currency { get; set; } = "ZAR";

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public decimal TotalDeposits { get; set; }

        public decimal TotalRefunds { get; set; }

        public decimal TotalPayments { get; set; }

        public decimal CalculatedBalance { get; set; }

        public List<WalletTransactionItemViewModel> Transactions { get; set; }
            = new List<WalletTransactionItemViewModel>();
    }

    public class WalletTransactionItemViewModel
    {
        public int Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string? Category { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public string? ExternalReference { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Currency { get; set; } = "ZAR";

        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}
