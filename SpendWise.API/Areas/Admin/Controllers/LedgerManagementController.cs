
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
    public class LedgerManagementController : Controller
    {
        private readonly AppDbContext _context;
        private readonly LedgerService _ledgerService;
        private readonly IAuditLogService _auditLogService;

        public LedgerManagementController(
            AppDbContext context,
            LedgerService ledgerService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _ledgerService = ledgerService;
            _auditLogService = auditLogService;
        }

        // ==========================================
        // LEDGER DASHBOARD
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var accounts = await _context.LedgerAccounts
                .AsNoTracking()
                .OrderBy(a => a.Code)
                .ToListAsync();

            var journals = await _context.LedgerJournals
                .AsNoTracking()
                .OrderByDescending(j => j.CreatedAt)
                .Take(100)
                .ToListAsync();

            var totalDebits = await _context.LedgerEntries
                .Where(e => e.EntryType == "Debit")
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var totalCredits = await _context.LedgerEntries
                .Where(e => e.EntryType == "Credit")
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var viewModel = new LedgerDashboardViewModel
            {
                Accounts = accounts,
                Journals = journals,
                TotalAccounts = accounts.Count,
                TotalJournals =
                    await _context.LedgerJournals.CountAsync(),
                TotalEntries =
                    await _context.LedgerEntries.CountAsync(),
                TotalDebits = totalDebits,
                TotalCredits = totalCredits
            };

            return View(viewModel);
        }

        // ==========================================
        // ENSURE SYSTEM ACCOUNTS
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnsureSystemAccounts()
        {
            try
            {
                await _ledgerService.EnsureSystemAccountsAsync();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Could not initialise system accounts. Check the application logs.";

                return RedirectToAction(nameof(Index));
            }

            // RECORD SUCCESSFUL INITIALISATION

            await _auditLogService.LogAsync(
                action: "Initialized",
                module: "Ledger",
                description:
                    "Administrator successfully checked and initialized system ledger accounts.",
                entityType: "LedgerAccount");

            TempData["SuccessMessage"] =
                "System ledger accounts were checked successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // BACKFILL COMPLETED WALLET TRANSACTIONS
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Backfill()
        {
            int journalsCreated;

            try
            {
                journalsCreated =
                    await _ledgerService
                        .BackfillCompletedWalletTransactionsAsync();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "Ledger backfill failed. Check the application logs.";

                return RedirectToAction(nameof(Index));
            }

            // RECORD SUCCESSFUL BACKFILL

            await _auditLogService.LogAsync(
                action: "Backfilled",
                module: "Ledger",
                description:
                    $"Administrator completed ledger backfill. {journalsCreated} journal(s) were created.",
                entityType: "LedgerJournal");

            TempData["SuccessMessage"] =
                $"{journalsCreated} ledger journal(s) created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // JOURNAL DETAILS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var journal = await _context.LedgerJournals
                .AsNoTracking()
                .Include(j => j.Entries)
                    .ThenInclude(e => e.LedgerAccount)
                .Include(j => j.WalletTransaction)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (journal == null)
            {
                return NotFound();
            }

            // RECORD JOURNAL DETAILS ACCESS

            await _auditLogService.LogAsync(
                action: "Viewed",
                module: "Ledger",
                description:
                    $"Administrator viewed ledger journal {journal.Id}.",
                entityType: "LedgerJournal",
                entityId: journal.Id.ToString());

            return View(journal);
        }

        // ==========================================
        // LEDGER ACCOUNTS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Accounts()
        {
            var accounts = await _context.LedgerAccounts
                .AsNoTracking()
                .Include(a => a.Wallet)
                .OrderBy(a => a.Code)
                .ToListAsync();

            return View(accounts);
        }
    }

    // ==========================================
    // LEDGER DASHBOARD VIEW MODEL
    // ==========================================

    public class LedgerDashboardViewModel
    {
        public List<LedgerAccount> Accounts { get; set; } = new();

        public List<LedgerJournal> Journals { get; set; } = new();

        public int TotalAccounts { get; set; }

        public int TotalJournals { get; set; }

        public int TotalEntries { get; set; }

        public decimal TotalDebits { get; set; }

        public decimal TotalCredits { get; set; }

        public bool IsBalanced => TotalDebits == TotalCredits;
    }
}
