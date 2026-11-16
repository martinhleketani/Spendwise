
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SpendWise.API.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator", AuthenticationSchemes = "AdminCookie")]
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult General()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Security()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            return View();
        }
    }
}
