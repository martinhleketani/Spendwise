
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
        private readonly WalletService _walletService;
        private readonly IAuditLogService _auditLogService;

        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration,
            PasswordResetService passwordResetService,
            IEmailService emailService,
            WalletService walletService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _configuration = configuration;
            _passwordResetService = passwordResetService;
            _emailService = emailService;
            _walletService = walletService;
            _auditLogService = auditLogService;

            _passwordHasher = new PasswordHasher<User>();
        }

        // =========================================================
        // REGISTER CUSTOMER AND AUTOMATICALLY CREATE WALLET
        // =========================================================

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string fullName = dto.FullName.Trim();

            string email = dto.Email.Trim().ToLowerInvariant();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                return Conflict(new
                {
                    message =
                        "An account with this email already exists."
                });
            }

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
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

                // Automatically create one wallet for this customer.
                var wallet =
                    await _walletService.GetOrCreateWalletAsync(
                        user.Id);

                // Record customer registration.
                // This uses the existing database transaction.
                await _auditLogService.LogAsyncForActorAsync(
                    actorType: "Customer",
                    actorId: user.Id,
                    actorName: user.FullName,
                    actorEmail: user.Email,
                    action: "Registered",
                    module: "Authentication",
                    description: "A new customer account was registered.",
                    entityType: "User",
                    entityId: user.Id.ToString());

                await dbTransaction.CommitAsync();

                return Ok(new
                {
                    message =
                        "Account and wallet created successfully.",

                    user = new
                    {
                        user.Id,
                        user.FullName,
                        user.Email
                    },

                    wallet = new
                    {
                        wallet.Id,
                        wallet.WalletNumber,
                        wallet.Currency,
                        wallet.Status
                    }
                });
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
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

            if (user == null || !user.IsActive)
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

            // Record successful customer login.
            // Explicit actor details are used because the JWT
            // has not yet authenticated the request.
            await _auditLogService.LogAsyncForActorAsync(
                actorType: "Customer",
                actorId: user.Id,
                actorName: user.FullName,
                actorEmail: user.Email,
                action: "Logged In",
                module: "Authentication",
                description: "Customer successfully logged into SpendWise.",
                entityType: "User",
                entityId: user.Id.ToString());

            var tokenResult = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login successful.",

                token = tokenResult.Token,

                expiresAt = tokenResult.ExpiresAt,

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
                dto.Email.Trim().ToLowerInvariant();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Email.ToLower() == email &&
                            u.IsActive);

            // Generic response: do not reveal whether an account exists.

            if (user == null)
            {
                return Ok(new
                {
                    message =
                        "If an account exists with this email, a password reset code will be sent."
                });
            }

            // Invalidate previous OTPs and reset tokens.

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

                previousCode.UsedAt ??= DateTime.UtcNow;

                previousCode.ResetTokenUsed = true;
            }

            // Generate a new six-digit OTP.

            string code =
                _passwordResetService.GenerateCode();

            var resetCode = new PasswordResetCode
            {
                UserId = user.Id,

                DeliveryMethod = "Email",

                ExpiresAt = DateTime.UtcNow.AddSeconds(60),

                IsUsed = false,

                AttemptCount = 0,

                MaxAttempts = 5,

                CreatedAt = DateTime.UtcNow,

                ResetTokenUsed = false
            };

            // Store only the OTP hash.

            resetCode.CodeHash =
                _passwordResetService.HashCode(
                    resetCode,
                    code);

            _context.PasswordResetCodes.Add(resetCode);

            await _context.SaveChangesAsync();

            // Send OTP to registered email.

            try
            {
                await _emailService.SendPasswordResetCodeAsync(
                    user.Email,
                    code);
            }
            catch
            {
                resetCode.IsUsed = true;

                resetCode.UsedAt = DateTime.UtcNow;

                resetCode.ResetTokenUsed = true;

                await _context.SaveChangesAsync();

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
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
                dto.Email.Trim().ToLowerInvariant();

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

            // Get latest unused email OTP.

            var resetCode =
                await _context.PasswordResetCodes
                    .Where(r =>
                        r.UserId == user.Id &&
                        r.DeliveryMethod == "Email" &&
                        !r.IsUsed)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

            if (resetCode == null)
            {
                return BadRequest(new
                {
                    message =
                        "Invalid or expired reset code."
                });
            }

            // Check 60-second expiry.

            if (_passwordResetService.IsExpired(resetCode))
            {
                resetCode.IsUsed = true;

                resetCode.UsedAt = DateTime.UtcNow;

                resetCode.ResetTokenUsed = true;

                await _context.SaveChangesAsync();

                return BadRequest(new
                {
                    message =
                        "Reset code has expired. Please request a new code."
                });
            }

            // Check maximum attempts.

            if (resetCode.AttemptCount >= resetCode.MaxAttempts)
            {
                resetCode.IsUsed = true;

                resetCode.UsedAt ??= DateTime.UtcNow;

                resetCode.ResetTokenUsed = true;

                await _context.SaveChangesAsync();

                return BadRequest(new
                {
                    message =
                        "Maximum verification attempts reached. Please request a new code."
                });
            }

            // Verify OTP.

            bool validCode =
                _passwordResetService.VerifyCode(
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
                    resetCode.IsUsed = true;

                    resetCode.UsedAt = DateTime.UtcNow;

                    resetCode.ResetTokenUsed = true;

                    attemptsRemaining = 0;
                }

                await _context.SaveChangesAsync();

                return BadRequest(new
                {
                    message =
                        attemptsRemaining == 0
                            ? "Maximum verification attempts reached. Please request a new code."
                            : "Invalid reset code.",

                    attemptsRemaining
                });
            }

            // OTP correct.

            resetCode.IsUsed = true;

            resetCode.UsedAt = DateTime.UtcNow;

            // Generate password reset token.

            string resetToken =
                _passwordResetService.GenerateResetToken();

            resetCode.ResetTokenHash =
                _passwordResetService.HashResetToken(
                    resetCode,
                    resetToken);

            resetCode.ResetTokenExpiresAt =
                DateTime.UtcNow.AddMinutes(10);

            resetCode.ResetTokenUsed = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Reset code verified successfully.",

                resetToken,

                resetTokenExpiresInMinutes = 10
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

            if (string.IsNullOrWhiteSpace(dto.ResetToken))
            {
                return BadRequest(new
                {
                    message = "Reset token is required."
                });
            }

            // Get active reset tokens.

            var possibleResetCodes =
                await _context.PasswordResetCodes
                    .Where(r =>
                        r.ResetTokenHash != null &&
                        !r.ResetTokenUsed)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

            PasswordResetCode? matchingResetCode = null;

            foreach (var resetCode in possibleResetCodes)
            {
                bool tokenMatches =
                    _passwordResetService.VerifyResetToken(
                        resetCode,
                        dto.ResetToken);

                if (tokenMatches)
                {
                    matchingResetCode = resetCode;
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

            // Check reset token expiry.

            if (matchingResetCode.ResetTokenExpiresAt == null ||
                DateTime.UtcNow >= matchingResetCode.ResetTokenExpiresAt)
            {
                matchingResetCode.ResetTokenUsed = true;

                await _context.SaveChangesAsync();

                return BadRequest(new
                {
                    message =
                        "Reset token has expired. Please request a new code."
                });
            }

            // Get user.

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id == matchingResetCode.UserId &&
                            u.IsActive);

            if (user == null)
            {
                return BadRequest(new
                {
                    message =
                        "Unable to reset password."
                });
            }

            // Hash and save new password.

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    dto.NewPassword);

            // Invalidate all existing JWTs.

            user.TokenVersion++;

            // Prevent reset token reuse.

            matchingResetCode.ResetTokenUsed = true;

            // Invalidate all other reset requests.

            var otherResetCodes =
                await _context.PasswordResetCodes
                    .Where(r =>
                        r.UserId == user.Id &&
                        r.Id != matchingResetCode.Id)
                    .ToListAsync();

            foreach (var resetCode in otherResetCodes)
            {
                resetCode.IsUsed = true;

                resetCode.ResetTokenUsed = true;

                resetCode.UsedAt ??= DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Record successful password reset.
            // Never include the password or reset token in the audit log.
            await _auditLogService.LogAsyncForActorAsync(
                actorType: "Customer",
                actorId: user.Id,
                actorName: user.FullName,
                actorEmail: user.Email,
                action: "Password Reset",
                module: "Authentication",
                description: "Customer successfully reset their account password.",
                entityType: "User",
                entityId: user.Id.ToString());

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
                ?? throw new InvalidOperationException(
                    "JWT signing key is missing.");

            string issuer =
                _configuration["Jwt:Issuer"]
                ?? "SpendWise.API";

            string audience =
                _configuration["Jwt:Audience"]
                ?? "SpendWise.App";

            int expiryMinutes =
                _configuration.GetValue<int?>(
                    "Jwt:ExpiryMinutes")
                ?? 60;

            DateTime expiresAt =
                DateTime.UtcNow.AddMinutes(expiryMinutes);

            var claims = new List<Claim>
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
                    Encoding.UTF8.GetBytes(jwtKey));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            string tokenString =
                new JwtSecurityTokenHandler()
                    .WriteToken(token);

            return (tokenString, expiresAt);
        }
    }
}
