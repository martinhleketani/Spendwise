using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SpendWise.API.Data;
using SpendWise.API.DTOs;
using SpendWise.API.Models;
using SpendWise.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SpendWise.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordResetService _passwordResetService;
        private readonly IEmailService _emailService;

        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration,
            PasswordResetService passwordResetService,
            IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _passwordResetService = passwordResetService;
            _emailService = emailService;

            _passwordHasher = new PasswordHasher<User>();
        }


        // =========================================================
        // REGISTER
        // =========================================================

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string fullName =
                dto.FullName.Trim();

            string email =
                dto.Email.Trim().ToLowerInvariant();

            bool emailExists =
                await _context.Users.AnyAsync(
                    u => u.Email.ToLower() == email);

            if (emailExists)
            {
                return Conflict(new
                {
                    message =
                        "An account with this email already exists."
                });
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                TokenVersion = 1,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    dto.Password);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Account created successfully.",

                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email
                }
            });
        }


        // =========================================================
        // LOGIN
        // =========================================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string email =
                dto.Email.Trim().ToLowerInvariant();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Email.ToLower() == email);

            if (user == null ||
                !user.IsActive)
            {
                return Unauthorized(new
                {
                    message =
                        "Invalid email or password."
                });
            }

            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    dto.Password);

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message =
                        "Invalid email or password."
                });
            }

            var tokenResult =
                GenerateJwtToken(user);

            return Ok(new
            {
                message =
                    "Login successful.",

                token =
                    tokenResult.Token,

                expiresAt =
                    tokenResult.ExpiresAt,

                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber
                }
            });
        }


        // =========================================================
        // FORGOT PASSWORD - EMAIL ONLY
        // =========================================================

        [HttpPost("forgot-password")]
        [EnableRateLimiting("ForgotPasswordPolicy")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string email =
                dto.Email
                    .Trim()
                    .ToLowerInvariant();

            // =====================================================
            // FIND ACTIVE USER
            // =====================================================

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Email.ToLower() == email &&
                            u.IsActive);

            // =====================================================
            // GENERIC RESPONSE
            //
            // Never reveal whether an email is registered.
            // =====================================================

            if (user == null)
            {
                return Ok(new
                {
                    message =
                        "If an account exists with this email, a password reset code will be sent."
                });
            }

            // =====================================================
            // INVALIDATE PREVIOUS OTPs AND RESET TOKENS
            // =====================================================

            var previousCodes =
                await _context.PasswordResetCodes
                    .Where(r =>
                        r.UserId == user.Id &&
                        (
                            !r.IsUsed ||
                            !r.ResetTokenUsed
                        ))
                    .ToListAsync();

            foreach (var previousCode in previousCodes)
            {
                previousCode.IsUsed = true;

                previousCode.UsedAt ??=
                    DateTime.UtcNow;

                previousCode.ResetTokenUsed =
                    true;
            }

            // =====================================================
            // GENERATE NEW 6-DIGIT OTP
            // =====================================================

            string code =
                _passwordResetService
                    .GenerateCode();

            var resetCode =
                new PasswordResetCode
                {
                    UserId =
                        user.Id,

                    DeliveryMethod =
                        "Email",

                    ExpiresAt =
                        DateTime.UtcNow
                            .AddSeconds(60),

                    IsUsed =
                        false,

                    AttemptCount =
                        0,

                    MaxAttempts =
                        5,

                    CreatedAt =
                        DateTime.UtcNow,

                    ResetTokenUsed =
                        false
                };

            // Store only the hash of the OTP.

            resetCode.CodeHash =
                _passwordResetService
                    .HashCode(
                        resetCode,
                        code);

            _context.PasswordResetCodes
                .Add(resetCode);

            await _context
                .SaveChangesAsync();

            // =====================================================
            // SEND OTP TO REGISTERED EMAIL
            // =====================================================

            try
            {
                await _emailService
                    .SendPasswordResetCodeAsync(
                        user.Email,
                        code);
            }
            catch
            {
                // If sending fails, invalidate the OTP.

                resetCode.IsUsed =
                    true;

                resetCode.UsedAt =
                    DateTime.UtcNow;

                resetCode.ResetTokenUsed =
                    true;

                await _context
                    .SaveChangesAsync();

                return StatusCode(
                    StatusCodes
                        .Status503ServiceUnavailable,
                    new
                    {
                        message =
                            "The password reset email could not be sent. Please try again."
                    });
            }

            // Never return the OTP.

            return Ok(new
            {
                message =
                    "If an account exists with this email, a password reset code will be sent."
            });
        }


        // =========================================================
        // VERIFY RESET CODE - EMAIL ONLY
        // =========================================================

        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCode(
            [FromBody] VerifyResetCodeDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string email =
                dto.Email
                    .Trim()
                    .ToLowerInvariant();

            // =====================================================
            // FIND ACTIVE USER
            // =====================================================

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Email.ToLower() == email &&
                            u.IsActive);

            if (user == null)
            {
                return BadRequest(new
                {
                    message =
                        "Invalid or expired reset code."
                });
            }

            // =====================================================
            // GET LATEST UNUSED EMAIL OTP
            // =====================================================

            var resetCode =
                await _context.PasswordResetCodes
                    .Where(r =>
                        r.UserId == user.Id &&
                        r.DeliveryMethod == "Email" &&
                        !r.IsUsed)
                    .OrderByDescending(
                        r => r.CreatedAt)
                    .FirstOrDefaultAsync();

            if (resetCode == null)
            {
                return BadRequest(new
                {
                    message =
                        "Invalid or expired reset code."
                });
            }

            // =====================================================
            // CHECK 60-SECOND EXPIRY
            // =====================================================

            if (_passwordResetService
                .IsExpired(resetCode))
            {
                resetCode.IsUsed =
                    true;

                resetCode.UsedAt =
                    DateTime.UtcNow;

                resetCode.ResetTokenUsed =
                    true;

                await _context
                    .SaveChangesAsync();

                return BadRequest(new
                {
                    message =
                        "Reset code has expired. Please request a new code."
                });
            }

            // =====================================================
            // CHECK MAXIMUM ATTEMPTS
            // =====================================================

            if (resetCode.AttemptCount >=
                resetCode.MaxAttempts)
            {
                resetCode.IsUsed =
                    true;

                resetCode.UsedAt ??=
                    DateTime.UtcNow;

                resetCode.ResetTokenUsed =
                    true;

                await _context
                    .SaveChangesAsync();

                return BadRequest(new
                {
                    message =
                        "Maximum verification attempts reached. Please request a new code."
                });
            }

            // =====================================================
            // VERIFY OTP
            // =====================================================

            bool validCode =
                _passwordResetService
                    .VerifyCode(
                        resetCode,
                        dto.Code);

            if (!validCode)
            {
                resetCode.AttemptCount++;

                int attemptsRemaining =
                    resetCode.MaxAttempts -
                    resetCode.AttemptCount;

                if (attemptsRemaining <= 0)
                {
                    resetCode.IsUsed =
                        true;

                    resetCode.UsedAt =
                        DateTime.UtcNow;

                    resetCode.ResetTokenUsed =
                        true;

                    attemptsRemaining =
                        0;
                }

                await _context
                    .SaveChangesAsync();

                return BadRequest(new
                {
                    message =
                        attemptsRemaining == 0
                            ? "Maximum verification attempts reached. Please request a new code."
                            : "Invalid reset code.",

                    attemptsRemaining
                });
            }

            // =====================================================
            // OTP CORRECT
            // =====================================================

            resetCode.IsUsed =
                true;

            resetCode.UsedAt =
                DateTime.UtcNow;

            // =====================================================
            // GENERATE PASSWORD RESET TOKEN
            // =====================================================

            string resetToken =
                _passwordResetService
                    .GenerateResetToken();

            resetCode.ResetTokenHash =
                _passwordResetService
                    .HashResetToken(
                        resetCode,
                        resetToken);

            resetCode.ResetTokenExpiresAt =
                DateTime.UtcNow
                    .AddMinutes(10);

            resetCode.ResetTokenUsed =
                false;

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Reset code verified successfully.",

                resetToken,

                resetTokenExpiresInMinutes =
                    10
            });
        }


        // =========================================================
        // RESET PASSWORD
        // =========================================================

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(
                dto.ResetToken))
            {
                return BadRequest(new
                {
                    message =
                        "Reset token is required."
                });
            }

            // =====================================================
            // GET ACTIVE RESET TOKENS
            // =====================================================

            var possibleResetCodes =
                await _context.PasswordResetCodes
                    .Where(r =>
                        r.ResetTokenHash != null &&
                        !r.ResetTokenUsed)
                    .OrderByDescending(
                        r => r.CreatedAt)
                    .ToListAsync();

            PasswordResetCode?
                matchingResetCode = null;

            foreach (var resetCode in possibleResetCodes)
            {
                bool tokenMatches =
                    _passwordResetService
                        .VerifyResetToken(
                            resetCode,
                            dto.ResetToken);

                if (tokenMatches)
                {
                    matchingResetCode =
                        resetCode;

                    break;
                }
            }

            if (matchingResetCode == null)
            {
                return BadRequest(new
                {
                    message =
                        "Invalid or expired reset token."
                });
            }

            // =====================================================
            // CHECK RESET TOKEN EXPIRY
            // =====================================================

            if (matchingResetCode
                    .ResetTokenExpiresAt == null ||
                DateTime.UtcNow >=
                    matchingResetCode
                        .ResetTokenExpiresAt)
            {
                matchingResetCode
                    .ResetTokenUsed = true;

                await _context
                    .SaveChangesAsync();

                return BadRequest(new
                {
                    message =
                        "Reset token has expired. Please request a new code."
                });
            }

            // =====================================================
            // GET USER
            // =====================================================

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id ==
                            matchingResetCode.UserId &&
                            u.IsActive);

            if (user == null)
            {
                return BadRequest(new
                {
                    message =
                        "Unable to reset password."
                });
            }

            // =====================================================
            // HASH AND SAVE NEW PASSWORD
            // =====================================================

            user.PasswordHash =
                _passwordHasher
                    .HashPassword(
                        user,
                        dto.NewPassword);

            // =====================================================
            // INVALIDATE ALL EXISTING JWTs
            // =====================================================

            user.TokenVersion++;

            // Prevent this reset token being used again.

            matchingResetCode
                .ResetTokenUsed = true;

            // =====================================================
            // INVALIDATE ALL OTHER RESET REQUESTS
            // =====================================================

            var otherResetCodes =
                await _context.PasswordResetCodes
                    .Where(r =>
                        r.UserId == user.Id &&
                        r.Id !=
                        matchingResetCode.Id)
                    .ToListAsync();

            foreach (var resetCode in otherResetCodes)
            {
                resetCode.IsUsed =
                    true;

                resetCode.ResetTokenUsed =
                    true;

                resetCode.UsedAt ??=
                    DateTime.UtcNow;
            }

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Password reset successfully. You can now log in with your new password."
            });
        }


        // =========================================================
        // JWT TOKEN
        // =========================================================

        private (
            string Token,
            DateTime ExpiresAt)
            GenerateJwtToken(User user)
        {
            string jwtKey =
                _configuration["Jwt:Key"]
                ?? throw new
                    InvalidOperationException(
                        "JWT signing key is missing.");

            string issuer =
                _configuration["Jwt:Issuer"]
                ?? "SpendWise.API";

            string audience =
                _configuration["Jwt:Audience"]
                ?? "SpendWise.App";

            int expiryMinutes =
                _configuration
                    .GetValue<int?>(
                        "Jwt:ExpiryMinutes")
                ?? 60;

            DateTime expiresAt =
                DateTime.UtcNow
                    .AddMinutes(
                        expiryMinutes);

            var claims =
                new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        user.Id.ToString()),

                    new Claim(
                        ClaimTypes.Name,
                        user.FullName),

                    new Claim(
                        ClaimTypes.Email,
                        user.Email),

                    new Claim(
                        "token_version",
                        user.TokenVersion.ToString()),

                    new Claim(
                        JwtRegisteredClaimNames.Jti,
                        Guid.NewGuid().ToString())
                };

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8
                        .GetBytes(jwtKey));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token =
                new JwtSecurityToken(
                    issuer:
                        issuer,

                    audience:
                        audience,

                    claims:
                        claims,

                    expires:
                        expiresAt,

                    signingCredentials:
                        credentials);

            string tokenString =
                new JwtSecurityTokenHandler()
                    .WriteToken(token);

            return (
                tokenString,
                expiresAt);
        }
    }
}