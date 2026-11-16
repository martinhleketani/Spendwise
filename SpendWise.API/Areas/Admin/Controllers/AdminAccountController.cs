
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Areas.Admin.Models;
using SpendWise.API.Data;
using SpendWise.API.Models;
using SpendWise.API.Services;
using System.Security.Claims;

namespace SpendWise.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminAccountController : Controller
    {
        private const string AdminScheme = "AdminCookie";

        private readonly AppDbContext _context;
        private readonly IPasswordHasher<AdminUser> _passwordHasher;
        private readonly ILogger<AdminAccountController> _logger;
        private readonly IAuditLogService _auditLogService;

        public AdminAccountController(
            AppDbContext context,
            IPasswordHasher<AdminUser> passwordHasher,
            ILogger<AdminAccountController> logger,
            IAuditLogService auditLogService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        // ==========================================
        // ADMIN LOGIN - GET
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            _logger.LogInformation(
                "ADMIN LOGIN GET ACTION REACHED.");

            var authenticationResult =
                await HttpContext.AuthenticateAsync(AdminScheme);

            if (authenticationResult.Succeeded &&
                authenticationResult.Principal?.IsInRole("Administrator") == true)
            {
                _logger.LogInformation(
                    "Administrator already authenticated.");

                return RedirectToAction(
                    "Index",
                    "AdminDashboard",
                    new { area = "Admin" });
            }

            return View(new AdminLoginViewModel());
        }

        // ==========================================
        // ADMIN LOGIN - POST
        // ==========================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            AdminLoginViewModel model)
        {
            _logger.LogInformation(
                "ADMIN LOGIN POST ACTION REACHED.");

            _logger.LogInformation(
                "Admin login form validation state: {IsValid}",
                ModelState.IsValid);

            // ======================================
            // CHECK FORM VALIDATION
            // ======================================

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    foreach (var item in error.Value.Errors)
                    {
                        _logger.LogWarning(
                            "Login validation error in {Field}: {Error}",
                            error.Key,
                            item.ErrorMessage);
                    }
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Please check your email and password.");

                return View(model);
            }

            // ======================================
            // NORMALIZE EMAIL
            // ======================================

            string email = model.Email.Trim().ToLowerInvariant();

            // ======================================
            // FIND ADMINISTRATOR ACCOUNT
            // ======================================

            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(a =>
                    a.Email.ToLower() == email);

            if (admin == null)
            {
                _logger.LogWarning(
                    "Admin login failed: account not found.");

                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            // ======================================
            // CHECK ACCOUNT STATUS
            // ======================================

            if (!admin.IsActive)
            {
                _logger.LogWarning(
                    "Admin login failed: account is inactive.");

                ModelState.AddModelError(
                    string.Empty,
                    "This administrator account is inactive.");

                return View(model);
            }

            // ======================================
            // CHECK PASSWORD HASH
            // ======================================

            if (string.IsNullOrWhiteSpace(admin.PasswordHash))
            {
                _logger.LogError(
                    "Admin login failed: password hash is missing.");

                ModelState.AddModelError(
                    string.Empty,
                    "Administrator password has not been configured.");

                return View(model);
            }

            // ======================================
            // PASSWORD VERIFICATION
            // ======================================

            PasswordVerificationResult verificationResult;

            try
            {
                verificationResult =
                    _passwordHasher.VerifyHashedPassword(
                        admin,
                        admin.PasswordHash,
                        model.Password);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Admin password verification encountered an error.");

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to verify your password. Please contact support.");

                return View(model);
            }

            // Never log the actual password or password hash.

            _logger.LogInformation(
                "Admin password verification result: {Result}",
                verificationResult);

            // ======================================
            // INCORRECT PASSWORD
            // ======================================

            if (verificationResult ==
                PasswordVerificationResult.Failed)
            {
                _logger.LogWarning(
                    "Admin login failed: incorrect password.");

                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            // ======================================
            // UPGRADE PASSWORD HASH IF REQUIRED
            // ======================================

            if (verificationResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                admin.PasswordHash =
                    _passwordHasher.HashPassword(
                        admin,
                        model.Password);
            }

            // ======================================
            // UPDATE LAST LOGIN
            // ======================================

            admin.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ======================================
            // CREATE ADMINISTRATOR CLAIMS
            // ======================================

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    admin.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    admin.FullName),

                new Claim(
                    ClaimTypes.Email,
                    admin.Email),

                new Claim(
                    ClaimTypes.Role,
                    "Administrator")
            };

            var identity = new ClaimsIdentity(
                claims,
                AdminScheme);

            var principal = new ClaimsPrincipal(identity);

            // ======================================
            // CREATE ADMINISTRATOR SESSION
            // ======================================

            var authenticationProperties =
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,

                    AllowRefresh = true,

                    ExpiresUtc = DateTimeOffset.UtcNow
                        .AddMinutes(30)
                };

            await HttpContext.SignInAsync(
                AdminScheme,
                principal,
                authenticationProperties);

            // Make the authenticated administrator
            // available to the AuditLogService.

            HttpContext.User = principal;

            _logger.LogInformation(
                "Administrator login successful. Admin ID: {AdminId}",
                admin.Id);

            // ======================================
            // RECORD SUCCESSFUL LOGIN
            // ======================================

            try
            {
                await _auditLogService.LogAsync(
                    action: "Logged In",
                    module: "Authentication",
                    description:
                        $"Administrator {admin.FullName} successfully logged in.",
                    entityType: "AdminUser",
                    entityId: admin.Id.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not record successful administrator login in audit logs. Admin ID: {AdminId}",
                    admin.Id);
            }

            // ======================================
            // REDIRECT TO ADMIN DASHBOARD
            // ======================================

            return RedirectToAction(
                "Index",
                "AdminDashboard",
                new { area = "Admin" });
        }

        // ==========================================
        // ADMIN LOGOUT
        // ==========================================

        [HttpPost]
        [Authorize(AuthenticationSchemes = AdminScheme)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Record logout before removing the
            // administrator authentication cookie.

            try
            {
                var adminId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                var adminName =
                    User.FindFirstValue(ClaimTypes.Name);

                await _auditLogService.LogAsync(
                    action: "Logged Out",
                    module: "Authentication",
                    description:
                        $"Administrator {adminName ?? "Administrator"} logged out.",
                    entityType: "AdminUser",
                    entityId: adminId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not record administrator logout in audit logs.");
            }

            await HttpContext.SignOutAsync(AdminScheme);

            _logger.LogInformation(
                "Administrator logged out.");

            return RedirectToAction(
                nameof(Login),
                "AdminAccount",
                new { area = "Admin" });
        }
    }
}
