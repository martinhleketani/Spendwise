
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpendWise.API.Data;
using SpendWise.API.Models;
using SpendWise.API.Services;

namespace SpendWise.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(
        AuthenticationSchemes = "AdminCookie",
        Roles = "Administrator")]
    public class MerchantManagementController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MerchantManagementController> _logger;
        private readonly IAuditLogService _auditLogService;

        public MerchantManagementController(
            AppDbContext context,
            ILogger<MerchantManagementController> logger,
            IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        // ==========================================
        // MERCHANT LIST
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Merchants
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(m =>
                    m.BusinessName.Contains(search) ||
                    (m.ContactPerson != null &&
                     m.ContactPerson.Contains(search)) ||
                    (m.Email != null &&
                     m.Email.Contains(search)) ||
                    (m.PhoneNumber != null &&
                     m.PhoneNumber.Contains(search)));
            }

            var merchants = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search ?? "";

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

            return View(merchants);
        }

        // ==========================================
        // CREATE MERCHANT - GET
        // ==========================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Merchant());
        }

        // ==========================================
        // CREATE MERCHANT - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("BusinessName,ContactPerson,Email,PhoneNumber,BusinessAddress")]
            Merchant merchant)
        {
            // STEP 1: CONFIRM POST REACHED CONTROLLER

            _logger.LogWarning(
                "========== MERCHANT REGISTRATION POST REACHED ==========");

            var connection = _context.Database.GetDbConnection();

            _logger.LogInformation(
                "Merchant registration database: {Database}. Server: {Server}",
                connection.Database,
                connection.DataSource);

            // STEP 2: CLEAN SUBMITTED VALUES

            merchant.BusinessName =
                merchant.BusinessName?.Trim() ?? string.Empty;

            merchant.ContactPerson =
                string.IsNullOrWhiteSpace(merchant.ContactPerson)
                    ? null
                    : merchant.ContactPerson.Trim();

            merchant.Email =
                string.IsNullOrWhiteSpace(merchant.Email)
                    ? null
                    : merchant.Email.Trim();

            merchant.PhoneNumber =
                string.IsNullOrWhiteSpace(merchant.PhoneNumber)
                    ? null
                    : merchant.PhoneNumber.Trim();

            merchant.BusinessAddress =
                string.IsNullOrWhiteSpace(merchant.BusinessAddress)
                    ? null
                    : merchant.BusinessAddress.Trim();

            // STEP 3: CHECK DUPLICATE EMAIL

            if (!string.IsNullOrWhiteSpace(merchant.Email))
            {
                bool emailExists = await _context.Merchants
                    .AnyAsync(m => m.Email == merchant.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        nameof(merchant.Email),
                        "This email address is already registered to another merchant.");
                }
            }

            // STEP 4: CHECK VALIDATION

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values
                             .SelectMany(v => v.Errors))
                {
                    _logger.LogWarning(
                        "Merchant validation error: {Error}",
                        error.ErrorMessage);
                }

                return View(merchant);
            }

            // STEP 5: SET SYSTEM-CONTROLLED VALUES

            merchant.Status = "Pending";
            merchant.CreatedAt = DateTime.UtcNow;
            merchant.UpdatedAt = null;

            // STEP 6: SAVE TO DATABASE

            try
            {
                _logger.LogInformation(
                    "Attempting to insert merchant: {BusinessName}",
                    merchant.BusinessName);

                _context.Merchants.Add(merchant);

                int rowsAffected = await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "MERCHANT SAVE COMPLETED. Rows affected: {RowsAffected}. Merchant ID: {MerchantId}",
                    rowsAffected,
                    merchant.Id);

                if (rowsAffected <= 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "The merchant could not be saved. Please try again.");

                    return View(merchant);
                }

                // VERIFY THAT THE RECORD CAN BE READ BACK

                var savedMerchant = await _context.Merchants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == merchant.Id);

                if (savedMerchant == null)
                {
                    _logger.LogError(
                        "Merchant {MerchantId} was saved but could not be retrieved.",
                        merchant.Id);

                    ModelState.AddModelError(
                        string.Empty,
                        "The merchant save could not be verified.");

                    return View(merchant);
                }

                _logger.LogInformation(
                    "MERCHANT VERIFIED IN DATABASE. ID: {MerchantId}, Name: {BusinessName}, Status: {Status}",
                    savedMerchant.Id,
                    savedMerchant.BusinessName,
                    savedMerchant.Status);

                // RECORD SUCCESSFUL MERCHANT REGISTRATION

                await _auditLogService.LogAsync(
                    action: "Created",
                    module: "Merchants",
                    description:
                        $"Administrator registered merchant {savedMerchant.BusinessName}. Initial status: Pending.",
                    entityType: "Merchant",
                    entityId: savedMerchant.Id.ToString());

                TempData["Success"] =
                    "Merchant registered successfully. The merchant is pending approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { area = "Admin" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "DATABASE ERROR while registering merchant {BusinessName}.",
                    merchant.BusinessName);

                _context.Entry(merchant).State =
                    EntityState.Detached;

                ModelState.AddModelError(
                    string.Empty,
                    "The merchant could not be saved to the database. Please check the application logs.");

                return View(merchant);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "UNEXPECTED ERROR while registering merchant.");

                if (_context.Entry(merchant).State != EntityState.Detached)
                {
                    _context.Entry(merchant).State =
                        EntityState.Detached;
                }

                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred while registering the merchant. Please check the application logs.");

                return View(merchant);
            }
        }

        // ==========================================
        // MERCHANT DETAILS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (merchant == null)
            {
                return NotFound();
            }

            // RECORD MERCHANT DETAILS ACCESS

            await _auditLogService.LogAsync(
                action: "Viewed",
                module: "Merchants",
                description:
                    $"Administrator viewed merchant details for {merchant.BusinessName}.",
                entityType: "Merchant",
                entityId: merchant.Id.ToString());

            return View(merchant);
        }

        // ==========================================
        // SUSPEND MERCHANT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id)
        {
            var merchant = await _context.Merchants
                .FirstOrDefaultAsync(m => m.Id == id);

            if (merchant == null)
            {
                return NotFound();
            }

            if (merchant.Status == "Closed")
            {
                TempData["Error"] =
                    "A closed merchant cannot be suspended.";

                return RedirectToAction(
                    nameof(Details),
                    new { area = "Admin", id });
            }

            merchant.Status = "Suspended";
            merchant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // RECORD SUCCESSFUL SUSPENSION

            await _auditLogService.LogAsync(
                action: "Suspended",
                module: "Merchants",
                description:
                    $"Administrator suspended merchant {merchant.BusinessName}.",
                entityType: "Merchant",
                entityId: merchant.Id.ToString());

            TempData["Success"] =
                "Merchant suspended successfully.";

            return RedirectToAction(
                nameof(Details),
                new { area = "Admin", id });
        }

        // ==========================================
        // REACTIVATE MERCHANT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var merchant = await _context.Merchants
                .FirstOrDefaultAsync(m => m.Id == id);

            if (merchant == null)
            {
                return NotFound();
            }

            if (merchant.Status == "Closed")
            {
                TempData["Error"] =
                    "A closed merchant cannot be reactivated.";

                return RedirectToAction(
                    nameof(Details),
                    new { area = "Admin", id });
            }

            merchant.Status = "Active";
            merchant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // RECORD SUCCESSFUL REACTIVATION

            await _auditLogService.LogAsync(
                action: "Reactivated",
                module: "Merchants",
                description:
                    $"Administrator reactivated merchant {merchant.BusinessName}.",
                entityType: "Merchant",
                entityId: merchant.Id.ToString());

            TempData["Success"] =
                "Merchant reactivated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { area = "Admin", id });
        }
    }
}
