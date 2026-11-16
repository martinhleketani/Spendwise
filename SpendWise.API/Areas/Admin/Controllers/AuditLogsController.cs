
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;

namespace SpendWise.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(
        Roles = "Administrator",
        AuthenticationSchemes = "AdminCookie")]
    public class AuditLogsController : Controller
    {
        private readonly AppDbContext _context;

        public AuditLogsController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // AUDIT LOGS INDEX
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? module,
            string? actionFilter,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1)
        {
            const int pageSize = 15;

            page = Math.Max(page, 1);

            // ======================================
            // BASE QUERY
            // ======================================

            var query = _context.AuditLogs
                .AsNoTracking()
                .AsQueryable();

            // ======================================
            // SEARCH
            // ======================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchTerm = search.Trim();

                query = query.Where(a =>
                    a.AdminName.Contains(searchTerm) ||
                    a.AdminEmail.Contains(searchTerm) ||
                    a.Description.Contains(searchTerm) ||
                    a.Module.Contains(searchTerm) ||
                    a.Action.Contains(searchTerm) ||
                    (a.EntityType != null &&
                     a.EntityType.Contains(searchTerm)) ||
                    (a.EntityId != null &&
                     a.EntityId.Contains(searchTerm)));
            }

            // ======================================
            // MODULE FILTER
            // ======================================

            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(a =>
                    a.Module == module);
            }

            // ======================================
            // ACTION FILTER
            // ======================================

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a =>
                    a.Action == actionFilter);
            }

            // ======================================
            // FROM DATE (UTC)
            // ======================================

            if (fromDate.HasValue)
            {
                DateTime startDate = DateTime.SpecifyKind(
                    fromDate.Value.Date,
                    DateTimeKind.Utc);

                query = query.Where(a =>
                    a.CreatedAt >= startDate);
            }

            // ======================================
            // TO DATE (UTC)
            // ======================================

            if (toDate.HasValue)
            {
                DateTime endDate = DateTime.SpecifyKind(
                    toDate.Value.Date.AddDays(1),
                    DateTimeKind.Utc);

                query = query.Where(a =>
                    a.CreatedAt < endDate);
            }

            // ======================================
            // STATISTICS
            // ======================================

            DateTime utcToday = DateTime.UtcNow.Date;
            DateTime utcTomorrow = utcToday.AddDays(1);

            ViewBag.TotalLogs =
                await _context.AuditLogs.CountAsync();

            ViewBag.TodayLogs =
                await _context.AuditLogs.CountAsync(a =>
                    a.CreatedAt >= utcToday &&
                    a.CreatedAt < utcTomorrow);

            ViewBag.AdminCount =
                await _context.AuditLogs
                    .Where(a => a.AdminUserId != null)
                    .Select(a => a.AdminUserId)
                    .Distinct()
                    .CountAsync();

            // ======================================
            // FILTERED RECORDS
            // ======================================

            int totalRecords = await query.CountAsync();

            ViewBag.FilteredLogs = totalRecords;
            ViewBag.TotalRecords = totalRecords;

            // ======================================
            // PAGINATION
            // ======================================

            int totalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var auditLogs = await query
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ======================================
            // FILTER OPTIONS
            // ======================================

            ViewBag.Modules = await _context.AuditLogs
                .AsNoTracking()
                .Select(a => a.Module)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            ViewBag.Actions = await _context.AuditLogs
                .AsNoTracking()
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            // ======================================
            // FILTER VALUES
            // ======================================

            ViewBag.Search = search ?? "";
            ViewBag.SelectedModule = module ?? "";
            ViewBag.SelectedAction = actionFilter ?? "";

            ViewBag.FromDate =
                fromDate?.ToString("yyyy-MM-dd") ?? "";

            ViewBag.ToDate =
                toDate?.ToString("yyyy-MM-dd") ?? "";

            // ======================================
            // PAGINATION VIEW DATA
            // ======================================

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            return View(auditLogs);
        }

        // ==========================================
        // AUDIT LOG DETAILS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var auditLog = await _context.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auditLog == null)
            {
                return NotFound();
            }

            return View(auditLog);
        }
    }
}
