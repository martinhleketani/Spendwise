
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using System.Security.Claims;

namespace SpendWise.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BudgetNotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BudgetNotificationsController(
            AppDbContext context)
        {
            _context = context;
        }

        // ============================================
        // GET ALL NOTIFICATIONS
        // GET: api/BudgetNotifications
        // ============================================

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var notifications = await _context
                .BudgetNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Category,
                    n.Threshold,
                    n.Message,
                    n.Month,
                    n.Year,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                count = notifications.Count,
                unreadCount = notifications.Count(n => !n.IsRead),
                notifications
            });
        }

        // ============================================
        // GET UNREAD NOTIFICATION COUNT
        // GET: api/BudgetNotifications/unread-count
        // ============================================

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            int count = await _context
                .BudgetNotifications
                .CountAsync(n =>
                    n.UserId == userId.Value &&
                    !n.IsRead);

            return Ok(new
            {
                unreadCount = count
            });
        }

        // ============================================
        // MARK ONE NOTIFICATION AS READ
        // PUT: api/BudgetNotifications/5/read
        // ============================================

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var notification = await _context
                .BudgetNotifications
                .FirstOrDefaultAsync(n =>
                    n.Id == id &&
                    n.UserId == userId.Value);

            if (notification == null)
                return NotFound(new
                {
                    message = "Notification not found."
                });

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Notification marked as read.",
                notificationId = notification.Id
            });
        }

        // ============================================
        // MARK ALL NOTIFICATIONS AS READ
        // PUT: api/BudgetNotifications/read-all
        // ============================================

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var notifications = await _context
                .BudgetNotifications
                .Where(n =>
                    n.UserId == userId.Value &&
                    !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "All notifications marked as read.",
                updatedCount = notifications.Count
            });
        }

        // ============================================
        // DELETE ONE NOTIFICATION
        // DELETE: api/BudgetNotifications/5
        // ============================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var notification = await _context
                .BudgetNotifications
                .FirstOrDefaultAsync(n =>
                    n.Id == id &&
                    n.UserId == userId.Value);

            if (notification == null)
            {
                return NotFound(new
                {
                    message = "Notification not found."
                });
            }

            _context.BudgetNotifications.Remove(notification);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ============================================
        // CLEAR ALL NOTIFICATIONS
        // DELETE: api/BudgetNotifications/clear-all
        // ============================================

        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAllNotifications()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var notifications = await _context
                .BudgetNotifications
                .Where(n => n.UserId == userId.Value)
                .ToListAsync();

            if (notifications.Count > 0)
            {
                _context.BudgetNotifications.RemoveRange(
                    notifications);

                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // ============================================
        // GET CURRENT USER ID FROM JWT
        // ============================================

        private int? GetCurrentUserId()
        {
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out int userId))
                return null;

            return userId;
        }
    }
}
