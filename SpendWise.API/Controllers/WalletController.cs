
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.Models;
using SpendWise.API.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SpendWise.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly WalletService _walletService;
        private readonly WalletPaymentService _paymentService;
        private readonly IWebHostEnvironment _environment;

        public WalletController(
            AppDbContext context,
            WalletService walletService,
            WalletPaymentService paymentService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _walletService = walletService;
            _paymentService = paymentService;
            _environment = environment;
        }

        // ==========================================
        // GET CUSTOMER WALLET
        // GET: api/Wallet
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid user authentication."
                });
            }

            var wallet = await _walletService.GetOrCreateWalletAsync(
                userId.Value);

            var walletWithTransactions =
                await _walletService.GetWalletAsync(userId.Value);

            if (walletWithTransactions == null)
            {
                return NotFound(new
                {
                    message = "Wallet not found."
                });
            }

            var completedTransactions =
                walletWithTransactions.Transactions
                    .Where(t =>
                        string.Equals(
                            t.Status,
                            "Completed",
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            decimal totalDeposits = completedTransactions
                .Where(t => t.Type == "Deposit")
                .Sum(t => t.Amount);

            decimal totalRefunds = completedTransactions
                .Where(t => t.Type == "Refund")
                .Sum(t => t.Amount);

            decimal totalPayments = completedTransactions
                .Where(t =>
                    t.Type == "Payment" ||
                    t.Type == "Withdrawal")
                .Sum(t => t.Amount);

            decimal calculatedBalance =
                totalDeposits + totalRefunds - totalPayments;

            return Ok(new
            {
                walletId = wallet.Id,
                walletNumber = wallet.WalletNumber,
                currency = wallet.Currency,
                status = wallet.Status,
                balance = calculatedBalance,
                createdAt = wallet.CreatedAt,
                updatedAt = wallet.UpdatedAt
            });
        }

        // ==========================================
        // CHECK WALLET STATUS
        // GET: api/Wallet/status
        // ==========================================

        [HttpGet("status")]
        public async Task<IActionResult> GetWalletStatus()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid user authentication."
                });
            }

            var wallet = await _context.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    w => w.UserId == userId.Value);

            if (wallet == null)
            {
                return NotFound(new
                {
                    message = "Wallet not found."
                });
            }

            bool isActive = string.Equals(
                wallet.Status,
                "Active",
                StringComparison.OrdinalIgnoreCase);

            return Ok(new
            {
                walletId = wallet.Id,
                walletNumber = wallet.WalletNumber,
                status = wallet.Status,
                isActive = isActive,
                message = isActive
                    ? "Your wallet is active."
                    : "Your wallet is suspended or inactive. Please contact support."
            });
        }

        // ==========================================
        // DEVELOPMENT SIMULATED DEPOSIT
        // POST: api/Wallet/test-deposit
        // ==========================================

        [HttpPost("test-deposit")]
        public async Task<IActionResult> CreateTestDeposit(
            [FromBody] TestDepositRequest request)
        {
            // This endpoint must never operate in production.
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid user authentication."
                });
            }

            if (request.Amount <= 0 ||
                request.Amount > 10000 ||
                decimal.Round(request.Amount, 2) != request.Amount)
            {
                return BadRequest(new
                {
                    message = "Enter an amount between R0.01 and R10,000.00 with no more than two decimal places."
                });
            }

            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(
                    w => w.UserId == userId.Value);

            if (wallet == null)
            {
                return NotFound(new
                {
                    message = "Wallet not found."
                });
            }

            if (!string.Equals(
                wallet.Status,
                "Active",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Cannot deposit into an inactive wallet."
                });
            }

            var deposit = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = "Deposit",
                Amount = request.Amount,
                Category = "Development Test",
                Description = "Simulated development test deposit",
                Reference = "SWTESTDEP" +
                    Guid.NewGuid().ToString("N").ToUpperInvariant(),
                ExternalReference = null,
                IdempotencyKey = null,
                Status = "Completed",
                Currency = wallet.Currency,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(deposit);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Simulated deposit created. No real money was transferred.",
                walletId = wallet.Id,
                transactionId = deposit.Id,
                reference = deposit.Reference,
                amount = deposit.Amount,
                currency = deposit.Currency,
                status = deposit.Status
            });
        }

        // ==========================================
        // MAKE WALLET PAYMENT
        // POST: api/Wallet/payments
        // ==========================================

        [HttpPost("payments")]
        public async Task<IActionResult> MakePayment(
            [FromBody] WalletPaymentRequest request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid user authentication."
                });
            }

            var result = await _paymentService.PayAsync(
                userId.Value,
                request.Amount,
                request.Description,
                request.IdempotencyKey,
                request.Category);

            // Idempotency key conflict.
            if (result.IsConflict)
            {
                return Conflict(new
                {
                    success = false,
                    message = result.Message
                });
            }

            // Payment failed.
            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            // Payment successful or already processed.
            return Ok(new
            {
                success = true,
                message = result.Message,
                reference = result.Reference,
                amount = result.Amount,
                balanceAfterPayment = result.BalanceAfterPayment,
                isDuplicate = result.IsDuplicate
            });
        }

        // ==========================================
        // GET CURRENT USER ID FROM JWT
        // ==========================================

        private int? GetCurrentUserId()
        {
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out int userId))
            {
                return null;
            }

            return userId;
        }
    }

    // ==============================================
    // WALLET PAYMENT REQUEST
    // ==============================================

    public class WalletPaymentRequest
    {
        [Range(typeof(decimal), "0.01", "9999999999999999.99")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(250, MinimumLength = 1)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string IdempotencyKey { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Category { get; set; }
    }

    // ==============================================
    // DEVELOPMENT TEST DEPOSIT REQUEST
    // ==============================================

    public class TestDepositRequest
    {
        [Range(typeof(decimal), "0.01", "10000.00")]
        public decimal Amount { get; set; }
    }
}
