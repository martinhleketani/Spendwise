
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.Models;

namespace SpendWise.API.Services
{
    public class WalletService
    {
        private readonly AppDbContext _context;

        public WalletService(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // CREATE OR RETRIEVE CUSTOMER WALLET
        // ==========================================

        public async Task<Wallet> GetOrCreateWalletAsync(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(
                    "Invalid customer ID.",
                    nameof(userId));
            }

            var existingWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (existingWallet != null)
            {
                return existingWallet;
            }

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == userId);

            if (!userExists)
            {
                throw new InvalidOperationException(
                    "The customer does not exist.");
            }

            var wallet = new Wallet
            {
                UserId = userId,
                WalletNumber = GenerateWalletNumber(),
                Currency = "ZAR",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Wallets.Add(wallet);

            try
            {
                await _context.SaveChangesAsync();

                return wallet;
            }
            catch (DbUpdateException)
            {
                _context.Entry(wallet).State =
                    EntityState.Detached;

                var walletCreatedByAnotherRequest =
                    await _context.Wallets
                        .FirstOrDefaultAsync(
                            w => w.UserId == userId);

                if (walletCreatedByAnotherRequest != null)
                {
                    return walletCreatedByAnotherRequest;
                }

                throw;
            }
        }

        // ==========================================
        // RETRIEVE CUSTOMER WALLET
        // ==========================================

        public async Task<Wallet?> GetWalletAsync(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(
                    "Invalid customer ID.",
                    nameof(userId));
            }

            var wallet = await _context.Wallets
                .AsNoTracking()
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(
                    w => w.UserId == userId);

            if (wallet != null)
            {
                wallet.Transactions = wallet.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();
            }

            return wallet;
        }

        // ==========================================
        // CHECK WHETHER CUSTOMER WALLET IS ACTIVE
        // ==========================================

        public async Task<Wallet> EnsureWalletIsActiveAsync(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(
                    "Invalid customer ID.",
                    nameof(userId));
            }

            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(
                    w => w.UserId == userId);

            if (wallet == null)
            {
                throw new InvalidOperationException(
                    "Customer wallet could not be found.");
            }

            if (!string.Equals(
                wallet.Status,
                "Active",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Your wallet is suspended or inactive. " +
                    "Please contact support.");
            }

            return wallet;
        }

        // ==========================================
        // GENERATE UNIQUE WALLET NUMBER
        // ==========================================

        private static string GenerateWalletNumber()
        {
            return "SW" +
                Guid.NewGuid()
                    .ToString("N")[..16]
                    .ToUpperInvariant();
        }
    }
}
