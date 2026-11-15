
using Microsoft.AspNetCore.Mvc;
using SpendWise.Admin.Models;

namespace SpendWise.Admin.Controllers
{
    public class AdminAccountController : Controller
    {
        // =========================================
        // ADMIN LOGIN PAGE
        // =========================================

        [HttpGet]
        public IActionResult Login()
        {
            return View(new AdminLoginViewModel());
        }

        // =========================================
        // PROCESS ADMIN LOGIN
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(AdminLoginViewModel model)
        {
            // Check that the submitted fields are valid.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Authentication will be connected later.
            // Do not use hardcoded administrator credentials.

            ModelState.AddModelError(
                string.Empty,
                "Administrator authentication has not been connected yet.");

            return View(model);
        }

        // =========================================
        // ADMIN LOGOUT
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Secure session and authentication
            // cleanup will be implemented when
            // the authentication system is added.

            return RedirectToAction(nameof(Login));
        }
    }
}
