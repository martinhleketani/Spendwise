
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.DTOs;
using System.Security.Claims;

namespace SpendWise.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized(new
                {
                    message = "Invalid user authentication."
                });

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound(new
                {
                    message = "User account not found."
                });

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.CreatedAt,
                user.IsActive
            });
        }

        // PUT: api/Profile
        [HttpPut]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateProfileDto request)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized(new
                {
                    message = "Invalid user authentication."
                });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound(new
                {
                    message = "User account not found."
                });

            var fullName = request.FullName?.Trim() ?? "";
            var email = request.Email?.Trim().ToLowerInvariant() ?? "";

            string? phoneNumber =
                string.IsNullOrWhiteSpace(request.PhoneNumber)
                    ? null
                    : request.PhoneNumber.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                return BadRequest(new
                {
                    message = "Full name is required."
                });

            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new
                {
                    message = "Email is required."
                });

            var emailAlreadyExists = await _context.Users
                .AnyAsync(u =>
                    u.Email.ToLower() == email &&
                    u.Id != user.Id);

            if (emailAlreadyExists)
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });

            if (phoneNumber != null)
            {
                var phoneAlreadyExists = await _context.Users
                    .AnyAsync(u =>
                        u.PhoneNumber == phoneNumber &&
                        u.Id != user.Id);

                if (phoneAlreadyExists)
                    return Conflict(new
                    {
                        message =
                            "An account with this phone number already exists."
                    });
            }

            user.FullName = fullName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.CreatedAt,
                    user.IsActive
                }
            });
        }

        // DELETE: api/Profile
        [HttpDelete]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized(new
                {
                    message = "Invalid user authentication."
                });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound(new
                {
                    message = "User account not found."
                });

            // Only the authenticated user's account is deleted.
            // Related records are handled by configured cascade deletion.
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Your account has been permanently deleted."
            });
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim))
                return null;

            if (!int.TryParse(userIdClaim, out var userId))
                return null;

            return userId;
        }
    }
}
