
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.Models;

namespace SpendWise.API.Services
{
    public class WalletPaymentService
    {
        private readonly AppDbContext _context;

        public WalletPaymentService(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // PROCESS WALLET PAYMENT
        // =====================================================

        public async Task<WalletPaymentResult> PayAsync(
            int userId,
            decimal amount,
            string description,
            string idempotencyKey,
            string? category = null)
        {
            // =============================================
            // VALIDATION
            // =============================================

            if (userId <= 0)
            {
                return WalletPaymentResult.Failed(
                    "Invalid customer account.");
            }

            if (amount <= 0)
            {
                return WalletPaymentResult.Failed(
                    "Payment amount must be greater than zero.");
            }

            if (amount != decimal.Round(amount, 2))
            {
                return WalletPaymentResult.Failed(
                    "Payment amount cannot have more than two decimal places.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return WalletPaymentResult.Failed(
                    "Payment description is required.");
            }

            description = description.Trim();

            if (description.Length > 250)
            {
                return WalletPaymentResult.Failed(
                    "Payment description cannot exceed 250 characters.");
            }

            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return WalletPaymentResult.Failed(
                    "An idempotency key is required.");
            }

            idempotencyKey = idempotencyKey.Trim();

            if (idempotencyKey.Length > 100)
            {
                return WalletPaymentResult.Failed(
                    "Idempotency key cannot exceed 100 characters.");
            }

            category = string.IsNullOrWhiteSpace(category)
                ? null
                : category.Trim();

            if (category != null && category.Length > 50)
            {
                return WalletPaymentResult.Failed(
                    "Payment category cannot exceed 50 characters.");
            }

            // =============================================
            // START DATABASE TRANSACTION
            // =============================================

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // =============================================
                // LOCK CUSTOMER WALLET
                // =============================================

                var wallet = await _context.Wallets
                    .FromSqlInterpolated(
                        $"SELECT * FROM Wallets WHERE UserId = {userId} FOR UPDATE")
                    .FirstOrDefaultAsync();

                if (wallet == null)
                {
                    await dbTransaction.RollbackAsync();

                    return WalletPaymentResult.Failed(
                        "Wallet could not be found.");
                }

                // =============================================
                // CHECK WALLET STATUS
                // =============================================

                if (!string.Equals(
                    wallet.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await dbTransaction.RollbackAsync();

                    return WalletPaymentResult.Failed(
                        "Your wallet is suspended or inactive. Please contact support.");
                }

                // =============================================
                // CHECK EXISTING IDEMPOTENCY KEY
                // =============================================

                var existingPayment = await _context.WalletTransactions
                    .FirstOrDefaultAsync(t =>
                        t.WalletId == wallet.Id &&
                        t.IdempotencyKey == idempotencyKey);

                if (existingPayment != null)
                {
                    bool samePayment =
                        existingPayment.Type == "Payment" &&
                        existingPayment.Amount == amount &&
                        string.Equals(
                            existingPayment.Description,
                            description,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            existingPayment.Category,
                            category,
                            StringComparison.Ordinal);

                    if (!samePayment)
                    {
                        await dbTransaction.RollbackAsync();

                        return WalletPaymentResult.Conflict(
                            "This idempotency key has already been used with different payment details.");
                    }

                    // =============================================
                    // RETURN ORIGINAL PAYMENT
                    // =============================================

                    var currentTransactions =
                        await _context.WalletTransactions
                            .AsNoTracking()
                            .Where(t => t.WalletId == wallet.Id)
                            .Where(t => t.Status == "Completed")
                            .ToListAsync();

                    decimal currentBalance =
                        CalculateBalance(currentTransactions);

                    await dbTransaction.CommitAsync();

                    return WalletPaymentResult.Succeeded(
                        "This payment was already processed. No additional payment was created.",
                        existingPayment.Reference,
                        existingPayment.Amount,
                        currentBalance,
                        true);
                }

                // =============================================
                // GET COMPLETED WALLET TRANSACTIONS
                // =============================================

                var completedTransactions =
                    await _context.WalletTransactions
                        .Where(t => t.WalletId == wallet.Id)
                        .Where(t => t.Status == "Completed")
                        .ToListAsync();

                // =============================================
                // CALCULATE AVAILABLE BALANCE
                // =============================================

                decimal availableBalance =
                    CalculateBalance(completedTransactions);

                // =============================================
                // CHECK SUFFICIENT FUNDS
                // =============================================

                if (availableBalance < amount)
                {
                    await dbTransaction.RollbackAsync();

                    return WalletPaymentResult.Failed(
                        "Insufficient wallet balance.");
                }

                // =============================================
                // CREATE PAYMENT TRANSACTION
                // =============================================

                string reference =
                    "SWPAY" + Guid.NewGuid().ToString("N");

                DateTime now = DateTime.UtcNow;

                var payment = new WalletTransaction
                {
                    WalletId = wallet.Id,

                    Type = "Payment",

                    Amount = amount,

                    Category = category,

                    Description = description,

                    Reference = reference,

                    ExternalReference = null,

                    IdempotencyKey = idempotencyKey,

                    Status = "Completed",

                    Currency = wallet.Currency,

                    CreatedAt = now,

                    CompletedAt = now
                };

                _context.WalletTransactions.Add(payment);

                await _context.SaveChangesAsync();

                // =============================================
                // COMMIT DATABASE TRANSACTION
                // =============================================

                await dbTransaction.CommitAsync();

                decimal remainingBalance =
                    availableBalance - amount;

                return WalletPaymentResult.Succeeded(
                    "Payment completed successfully.",
                    reference,
                    amount,
                    remainingBalance);
            }
            catch
            {
                await dbTransaction.RollbackAsync();

                throw;
            }
        }

        // =====================================================
        // CALCULATE WALLET BALANCE
        // =====================================================

        private static decimal CalculateBalance(
            List<WalletTransaction> transactions)
        {
            decimal totalDeposits = transactions
                .Where(t => t.Type == "Deposit")
                .Sum(t => t.Amount);

            decimal totalRefunds = transactions
                .Where(t => t.Type == "Refund")
                .Sum(t => t.Amount);

            decimal totalPayments = transactions
                .Where(t =>
                    t.Type == "Payment" ||
                    t.Type == "Withdrawal")
                .Sum(t => t.Amount);

            return totalDeposits +
                   totalRefunds -
                   totalPayments;
        }
    }

    // =========================================================
    // PAYMENT RESULT
    // =========================================================

    public class WalletPaymentResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? Reference { get; set; }

        public decimal Amount { get; set; }

        public decimal BalanceAfterPayment { get; set; }

        public bool IsDuplicate { get; set; }

        public bool IsConflict { get; set; }

        public static WalletPaymentResult Failed(string message)
        {
            return new WalletPaymentResult
            {
                Success = false,
                Message = message
            };
        }

        public static WalletPaymentResult Conflict(string message)
        {
            return new WalletPaymentResult
            {
                Success = false,
                IsConflict = true,
                Message = message
            };
        }

        public static WalletPaymentResult Succeeded(
            string message,
            string reference,
            decimal amount,
            decimal remainingBalance,
            bool isDuplicate = false)
        {
            return new WalletPaymentResult
            {
                Success = true,
                Message = message,
                Reference = reference,
                Amount = amount,
                BalanceAfterPayment = remainingBalance,
                IsDuplicate = isDuplicate
            };
        }
    }
}
