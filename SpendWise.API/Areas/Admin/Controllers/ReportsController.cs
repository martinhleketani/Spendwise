
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using System.Text.Json;

namespace SpendWise.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(
        AuthenticationSchemes = "AdminCookie",
        Roles = "Administrator")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // REPORTS DASHBOARD
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers =
                await _context.Users.CountAsync();

            ViewBag.TotalTransactions =
                await _context.Transactions.CountAsync();

            ViewBag.TotalWalletTransactions =
                await _context.WalletTransactions.CountAsync();

            ViewBag.TotalWallets =
                await _context.Wallets.CountAsync();

            ViewBag.TotalMerchants =
                await _context.Merchants.CountAsync();

            ViewBag.TotalLedgerAccounts =
                await _context.LedgerAccounts.CountAsync();

            ViewBag.TotalLedgerJournals =
                await _context.LedgerJournals.CountAsync();

            ViewBag.TotalLedgerEntries =
                await _context.LedgerEntries.CountAsync();

            return View();
        }

        // ==========================================
        // USER REPORTS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> UserReports()
        {
            ViewBag.TotalUsers =
                await _context.Users.CountAsync();

            ViewBag.ActiveUsers =
                await _context.Users.CountAsync(u => u.IsActive);

            ViewBag.InactiveUsers =
                await _context.Users.CountAsync(u => !u.IsActive);

            ViewBag.RecentUsers =
                await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(100)
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Email,
                        u.PhoneNumber,
                        u.IsActive,
                        u.CreatedAt
                    })
                    .ToListAsync();

            // Registrations over the last six months.
            var startDate = new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                1).AddMonths(-5);

            var registrationRecords =
                await _context.Users
                    .Where(u => u.CreatedAt >= startDate)
                    .GroupBy(u => new
                    {
                        u.CreatedAt.Year,
                        u.CreatedAt.Month
                    })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Count = g.Count()
                    })
                    .ToListAsync();

            var registrationDictionary =
                registrationRecords.ToDictionary(
                    x => $"{x.Year}-{x.Month}",
                    x => x.Count);

            var registrationLabels = new List<string>();
            var registrationValues = new List<int>();

            for (int i = 0; i < 6; i++)
            {
                var month = startDate.AddMonths(i);
                var key = $"{month.Year}-{month.Month}";

                registrationLabels.Add(month.ToString("MMM yyyy"));

                registrationValues.Add(
                    registrationDictionary.ContainsKey(key)
                        ? registrationDictionary[key]
                        : 0);
            }

            ViewBag.RegistrationChart =
                JsonSerializer.Serialize(new
                {
                    labels = registrationLabels,
                    values = registrationValues
                });

            return View();
        }

        // ==========================================
        // TRANSACTION REPORTS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> TransactionReports()
        {
            ViewBag.TotalTransactions =
                await _context.Transactions.CountAsync();

            ViewBag.TotalTransactionAmount =
                await _context.Transactions
                    .Select(t => (decimal?)t.Amount)
                    .SumAsync() ?? 0m;

            ViewBag.AverageTransactionAmount =
                await _context.Transactions
                    .Select(t => (decimal?)t.Amount)
                    .AverageAsync() ?? 0m;

            ViewBag.TransactionTypes =
                await _context.Transactions
                    .GroupBy(t => t.Type)
                    .Select(g => new
                    {
                        Name = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(t => t.Amount)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

            ViewBag.TransactionCategories =
                await _context.Transactions
                    .GroupBy(t => t.Category ?? "Uncategorised")
                    .Select(g => new
                    {
                        Name = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(t => t.Amount)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

            ViewBag.RecentTransactions =
                await _context.Transactions
                    .Include(t => t.User)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(100)
                    .ToListAsync();

            var typeChart =
                await _context.Transactions
                    .GroupBy(t => t.Type)
                    .Select(g => new
                    {
                        Name = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

            ViewBag.TransactionTypeChart =
                JsonSerializer.Serialize(new
                {
                    labels = typeChart.Select(x => x.Name),
                    values = typeChart.Select(x => x.Count)
                });

            return View();
        }

        // ==========================================
        // WALLET REPORTS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> WalletReports()
        {
            ViewBag.TotalWallets =
                await _context.Wallets.CountAsync();

            ViewBag.ActiveWallets =
                await _context.Wallets.CountAsync(
                    w => w.Status == "Active");

            ViewBag.SuspendedWallets =
                await _context.Wallets.CountAsync(
                    w => w.Status == "Suspended");

            ViewBag.ClosedWallets =
                await _context.Wallets.CountAsync(
                    w => w.Status == "Closed");

            ViewBag.TotalWalletTransactions =
                await _context.WalletTransactions.CountAsync();

            ViewBag.PendingWalletTransactions =
                await _context.WalletTransactions.CountAsync(
                    t => t.Status == "Pending");

            ViewBag.CompletedWalletTransactions =
                await _context.WalletTransactions.CountAsync(
                    t => t.Status == "Completed");

            ViewBag.FailedWalletTransactions =
                await _context.WalletTransactions.CountAsync(
                    t => t.Status == "Failed");

            ViewBag.ReversedWalletTransactions =
                await _context.WalletTransactions.CountAsync(
                    t => t.Status == "Reversed");

            // Completed financial movements, grouped by currency
            // and type. Amounts are not mixed across currencies.

            ViewBag.CompletedWalletTotals =
                await _context.WalletTransactions
                    .Where(t => t.Status == "Completed")
                    .GroupBy(t => new
                    {
                        t.Currency,
                        t.Type
                    })
                    .Select(g => new
                    {
                        g.Key.Currency,
                        g.Key.Type,
                        TransactionCount = g.Count(),
                        TotalAmount = g.Sum(t => t.Amount)
                    })
                    .OrderBy(x => x.Currency)
                    .ThenBy(x => x.Type)
                    .ToListAsync();

            ViewBag.WalletList =
                await _context.Wallets
                    .Include(w => w.User)
                    .OrderByDescending(w => w.CreatedAt)
                    .Take(100)
                    .ToListAsync();

            ViewBag.RecentWalletTransactions =
                await _context.WalletTransactions
                    .Include(t => t.Wallet)
                        .ThenInclude(w => w.User)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(100)
                    .ToListAsync();

            var walletStatusData =
                await _context.Wallets
                    .GroupBy(w => w.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

            ViewBag.WalletStatusChart =
                JsonSerializer.Serialize(new
                {
                    labels = walletStatusData.Select(x => x.Status),
                    values = walletStatusData.Select(x => x.Count)
                });

            return View();
        }

        // ==========================================
        // MERCHANT REPORTS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> MerchantReports()
        {
            ViewBag.TotalMerchants =
                await _context.Merchants.CountAsync();

            ViewBag.ActiveMerchants =
                await _context.Merchants.CountAsync(
                    m => m.Status == "Active");

            ViewBag.PendingMerchants =
                await _context.Merchants.CountAsync(
                    m => m.Status == "Pending");

            ViewBag.SuspendedMerchants =
                await _context.Merchants.CountAsync(
                    m => m.Status == "Suspended");

            ViewBag.ClosedMerchants =
                await _context.Merchants.CountAsync(
                    m => m.Status == "Closed");

            ViewBag.MerchantList =
                await _context.Merchants
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

            var merchantStatusData =
                await _context.Merchants
                    .GroupBy(m => m.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

            ViewBag.MerchantStatusChart =
                JsonSerializer.Serialize(new
                {
                    labels = merchantStatusData.Select(x => x.Status),
                    values = merchantStatusData.Select(x => x.Count)
                });

            return View();
        }

        // ==========================================
        // LEDGER REPORTS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> LedgerReports()
        {
            ViewBag.TotalLedgerAccounts =
                await _context.LedgerAccounts.CountAsync();

            ViewBag.ActiveLedgerAccounts =
                await _context.LedgerAccounts.CountAsync(
                    a => a.IsActive);

            ViewBag.InactiveLedgerAccounts =
                await _context.LedgerAccounts.CountAsync(
                    a => !a.IsActive);

            ViewBag.SystemAccounts =
                await _context.LedgerAccounts.CountAsync(
                    a => a.IsSystemAccount);

            ViewBag.CustomerAccounts =
                await _context.LedgerAccounts.CountAsync(
                    a => !a.IsSystemAccount);

            ViewBag.TotalLedgerJournals =
                await _context.LedgerJournals.CountAsync();

            ViewBag.DraftJournals =
                await _context.LedgerJournals.CountAsync(
                    j => j.Status == "Draft");

            ViewBag.PostedJournals =
                await _context.LedgerJournals.CountAsync(
                    j => j.Status == "Posted");

            ViewBag.ReversedJournals =
                await _context.LedgerJournals.CountAsync(
                    j => j.Status == "Reversed");

            ViewBag.TotalLedgerEntries =
                await _context.LedgerEntries.CountAsync();

            // Debit and credit totals for posted journals,
            // separated by currency.

            ViewBag.PostedLedgerTotals =
                await _context.LedgerEntries
                    .Where(e => e.LedgerJournal.Status == "Posted")
                    .GroupBy(e => new
                    {
                        e.LedgerJournal.Currency,
                        e.EntryType
                    })
                    .Select(g => new
                    {
                        g.Key.Currency,
                        g.Key.EntryType,
                        EntryCount = g.Count(),
                        TotalAmount = g.Sum(e => e.Amount)
                    })
                    .OrderBy(x => x.Currency)
                    .ThenBy(x => x.EntryType)
                    .ToListAsync();

            ViewBag.LedgerAccounts =
                await _context.LedgerAccounts
                    .Include(a => a.Wallet)
                    .OrderBy(a => a.Code)
                    .ToListAsync();

            // Load recent journals and their entries.
            var journals =
                await _context.LedgerJournals
                    .Include(j => j.Entries)
                        .ThenInclude(e => e.LedgerAccount)
                    .OrderByDescending(j => j.CreatedAt)
                    .Take(100)
                    .ToListAsync();

            // Calculate debit and credit totals per journal.
            ViewBag.LedgerJournals =
                journals.Select(j => new
                {
                    j.Id,
                    j.Reference,
                    j.Description,
                    j.Currency,
                    j.Status,
                    j.WalletTransactionId,
                    j.CreatedAt,
                    j.PostedAt,

                    DebitTotal = j.Entries
                        .Where(e => e.EntryType == "Debit")
                        .Sum(e => e.Amount),

                    CreditTotal = j.Entries
                        .Where(e => e.EntryType == "Credit")
                        .Sum(e => e.Amount),

                    IsBalanced =
                        j.Entries
                            .Where(e => e.EntryType == "Debit")
                            .Sum(e => e.Amount)
                        ==
                        j.Entries
                            .Where(e => e.EntryType == "Credit")
                            .Sum(e => e.Amount)
                })
                .ToList();

            return View();
        }
    }
}
