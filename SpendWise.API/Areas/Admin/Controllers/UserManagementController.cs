
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
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
    public class UserManagementController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;

        private const int PageSize = 10;

        public UserManagementController(
            AppDbContext context,
            IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // ==========================================
        // CUSTOMER DIRECTORY
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? status = "all",
            int page = 1)
        {
            search = search?.Trim() ?? string.Empty;

            status = (status ?? "all")
                .Trim()
                .ToLowerInvariant();

            if (status != "active" &&
                status != "inactive")
            {
                status = "all";
            }

            var query = _context.Users.AsNoTracking();

            // SEARCH

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search));
            }

            // STATUS FILTER

            if (status == "active")
            {
                query = query.Where(u => u.IsActive);
            }
            else if (status == "inactive")
            {
                query = query.Where(u => !u.IsActive);
            }

            // FILTERED COUNT

            int filteredUsers = await query.CountAsync();

            // PAGINATION

            int totalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    filteredUsers / (double)PageSize));

            page = Math.Clamp(page, 1, totalPages);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ThenByDescending(u => u.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(u => new UserManagementListItem
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            // ACCOUNT STATISTICS

            int totalUsers = await _context.Users.CountAsync();

            int activeUsers = await _context.Users.CountAsync(
                u => u.IsActive);

            int inactiveUsers = await _context.Users.CountAsync(
                u => !u.IsActive);

            // VIEW MODEL

            var model = new UserManagementIndexViewModel
            {
                Users = users,

                Search = search,
                Status = status,

                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,

                FilteredUsers = filteredUsers,

                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = PageSize
            };

            return View(model);
        }

        // ==========================================
        // CUSTOMER DETAILS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UserManagementListItem
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            // RECORD CUSTOMER DETAILS ACCESS

            await _auditLogService.LogAsync(
                action: "Viewed",
                module: "Users",
                description:
                    $"Administrator viewed customer details for {user.FullName} ({user.Email}).",
                entityType: "User",
                entityId: user.Id.ToString());

            return View(user);
        }
    }

    // ==========================================
    // CUSTOMER LIST ITEM
    // ==========================================

    public class UserManagementListItem
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }
    }

    // ==========================================
    // CUSTOMER DIRECTORY VIEW MODEL
    // ==========================================

    public class UserManagementIndexViewModel
    {
        public List<UserManagementListItem> Users { get; set; } = new();

        public string Search { get; set; } = string.Empty;

        public string Status { get; set; } = "all";

        public int TotalUsers { get; set; }

        public int ActiveUsers { get; set; }

        public int InactiveUsers { get; set; }

        public int FilteredUsers { get; set; }

        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        public int PageSize { get; set; }

        public int FirstItem =>
            FilteredUsers == 0
                ? 0
                : ((CurrentPage - 1) * PageSize) + 1;

        public int LastItem =>
            Math.Min(CurrentPage * PageSize, FilteredUsers);
    }
}
