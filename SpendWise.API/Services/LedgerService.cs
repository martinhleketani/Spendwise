
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Data;
using SpendWise.API.Models;

namespace SpendWise.API.Services
{
    public class LedgerService
    {
        private readonly AppDbContext _context;

        public LedgerService(AppDbContext context)
        {
            _context = context;
        }

        // ================================================
        // INITIALISE SYSTEM ACCOUNTS
        // ================================================

        public async Task EnsureSystemAccountsAsync()
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                await GetOrCreateSystemAccountAsync(
                    "TEST_FUNDING_ZAR",
                    "Development Test Funding",
                    "Asset",
                    "ZAR");

                await GetOrCreateSystemAccountAsync(
                    "DEPOSIT_CLEARING_ZAR",
                    "Deposit Clearing",
                    "Asset",
                    "ZAR");

                await GetOrCreateSystemAccountAsync(
                    "MERCHANT_CLEARING_ZAR",
                    "Merchant Payment Clearing",
                    "Liability",
                    "ZAR");

                await GetOrCreateSystemAccountAsync(
                    "WITHDRAWAL_CLEARING_ZAR",
                    "Withdrawal Clearing",
                    "Liability",
                    "ZAR");

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ================================================
        // CREATE OR RETRIEVE SYSTEM ACCOUNT
        // ================================================

        private async Task<LedgerAccount> GetOrCreateSystemAccountAsync(
            string code,
            string name,
            string accountType,
            string currency)
        {
            var account = await _context.LedgerAccounts
                .FirstOrDefaultAsync(a => a.Code == code);

            if (account != null)
            {
                if (!account.IsSystemAccount ||
                    account.Currency != currency ||
                    account.AccountType != accountType ||
                    account.WalletId != null)
                {
                    throw new InvalidOperationException(
                        $"System account {code} has invalid configuration.");
                }

                account.IsActive = true;

                return account;
            }

            account = new LedgerAccount
            {
                Code = code,
                Name = name,
                AccountType = accountType,
                Currency = currency,
                WalletId = null,
                IsSystemAccount = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.LedgerAccounts.AddAsync(account);

            return account;
        }

        // ================================================
        // ENSURE WALLET LEDGER ACCOUNT
        // ================================================

        public async Task<LedgerAccount> EnsureWalletLedgerAccountAsync(
            int walletId)
        {
            if (walletId <= 0)
            {
                throw new ArgumentException("Invalid wallet ID.");
            }

            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.Id == walletId);

            if (wallet == null)
            {
                throw new InvalidOperationException(
                    "Wallet does not exist.");
            }

            var account = await _context.LedgerAccounts
                .FirstOrDefaultAsync(a => a.WalletId == walletId);

            if (account != null)
            {
                ValidateWalletAccount(account, wallet);
                return account;
            }

            account = CreateWalletAccount(wallet);

            await _context.LedgerAccounts.AddAsync(account);
            await _context.SaveChangesAsync();

            return account;
        }

        private static void ValidateWalletAccount(
            LedgerAccount account,
            Wallet wallet)
        {
            if (account.Currency != wallet.Currency ||
                account.IsSystemAccount ||
                account.AccountType != "Liability" ||
                account.Code != $"WALLET_{wallet.Id}_{wallet.Currency}")
            {
                throw new InvalidOperationException(
                    "Wallet ledger account configuration is invalid.");
            }
        }

        private static LedgerAccount CreateWalletAccount(Wallet wallet)
        {
            return new LedgerAccount
            {
                Code = $"WALLET_{wallet.Id}_{wallet.Currency}",
                Name = $"Customer Wallet {wallet.WalletNumber}",
                AccountType = "Liability",
                Currency = wallet.Currency,
                WalletId = wallet.Id,
                IsSystemAccount = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        // ================================================
        // BACKFILL COMPLETED WALLET TRANSACTIONS
        // ================================================

        public async Task<int> BackfillCompletedWalletTransactionsAsync()
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                await EnsureSystemAccountsInsideTransactionAsync();

                var completedTransactions =
                    await _context.WalletTransactions
                        .AsNoTracking()
                        .Where(t => t.Status == "Completed")
                        .OrderBy(t => t.Id)
                        .ToListAsync();

                int journalsCreated = 0;

                foreach (var walletTransaction in completedTransactions)
                {
                    bool alreadyExists =
                        await _context.LedgerJournals
                            .AnyAsync(j =>
                                j.WalletTransactionId ==
                                walletTransaction.Id);

                    if (alreadyExists)
                    {
                        continue;
                    }

                    ValidateWalletTransaction(walletTransaction);

                    var wallet = await _context.Wallets
                        .FirstOrDefaultAsync(
                            w => w.Id == walletTransaction.WalletId);

                    if (wallet == null)
                    {
                        throw new InvalidOperationException(
                            $"Wallet for transaction {walletTransaction.Id} was not found.");
                    }

                    if (wallet.Currency != walletTransaction.Currency)
                    {
                        throw new InvalidOperationException(
                            $"Currency mismatch for transaction {walletTransaction.Id}.");
                    }

                    var walletAccount =
                        await GetOrCreateWalletAccountInsideTransactionAsync(
                            wallet);

                    var debitAccount =
                        await GetDebitAccountAsync(walletTransaction);

                    var creditAccount =
                        await GetCreditAccountAsync(walletTransaction);

                    if (debitAccount.Id == creditAccount.Id)
                    {
                        throw new InvalidOperationException(
                            "Debit and credit accounts cannot be identical.");
                    }

                    ValidateJournalAccounts(
                        walletTransaction,
                        walletAccount,
                        debitAccount,
                        creditAccount);

                    var amount = walletTransaction.Amount;

                    var journal = new LedgerJournal
                    {
                        WalletTransactionId = walletTransaction.Id,
                        Reference = $"WLT-{walletTransaction.Id}",
                        Description = walletTransaction.Description,
                        Currency = walletTransaction.Currency,
                        Status = "Posted",
                        CreatedAt = walletTransaction.CreatedAt,
                        PostedAt = walletTransaction.CompletedAt
                            ?? walletTransaction.CreatedAt
                    };

                    journal.Entries.Add(new LedgerEntry
                    {
                        LedgerAccount = debitAccount,
                        EntryType = "Debit",
                        Amount = amount,
                        CreatedAt = journal.PostedAt.Value
                    });

                    journal.Entries.Add(new LedgerEntry
                    {
                        LedgerAccount = creditAccount,
                        EntryType = "Credit",
                        Amount = amount,
                        CreatedAt = journal.PostedAt.Value
                    });

                    ValidateJournal(journal);

                    await _context.LedgerJournals.AddAsync(journal);
                    await _context.SaveChangesAsync();

                    journalsCreated++;
                }

                await transaction.CommitAsync();

                return journalsCreated;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ================================================
        // VALIDATE WALLET TRANSACTION
        // ================================================

        private static void ValidateWalletTransaction(
            WalletTransaction walletTransaction)
        {
            if (walletTransaction.Status != "Completed")
            {
                throw new InvalidOperationException(
                    "Only completed transactions can be posted.");
            }

            if (walletTransaction.Amount <= 0 ||
                decimal.Round(walletTransaction.Amount, 2) !=
                walletTransaction.Amount)
            {
                throw new InvalidOperationException(
                    $"Transaction {walletTransaction.Id} has an invalid amount.");
            }

            if (walletTransaction.Currency != "ZAR")
            {
                throw new InvalidOperationException(
                    $"Transaction {walletTransaction.Id} has an unsupported currency.");
            }

            if (string.IsNullOrWhiteSpace(walletTransaction.Description))
            {
                throw new InvalidOperationException(
                    $"Transaction {walletTransaction.Id} has no description.");
            }

            if (string.IsNullOrWhiteSpace(walletTransaction.Type))
            {
                throw new InvalidOperationException(
                    $"Transaction {walletTransaction.Id} has no type.");
            }
        }

        // ================================================
        // VALIDATE ACCOUNT MAPPINGS
        // ================================================

        private static void ValidateJournalAccounts(
            WalletTransaction transaction,
            LedgerAccount walletAccount,
            LedgerAccount debitAccount,
            LedgerAccount creditAccount)
        {
            var type = transaction.Type.Trim().ToLowerInvariant();

            switch (type)
            {
                case "deposit":
                case "refund":
                    if (creditAccount.Id != walletAccount.Id)
                    {
                        throw new InvalidOperationException(
                            "The wallet must be credited for deposits and refunds.");
                    }
                    break;

                case "payment":
                case "withdrawal":
                    if (debitAccount.Id != walletAccount.Id)
                    {
                        throw new InvalidOperationException(
                            "The wallet must be debited for payments and withdrawals.");
                    }
                    break;
            }
        }

        // ================================================
        // VALIDATE JOURNAL
        // ================================================

        private static void ValidateJournal(LedgerJournal journal)
        {
            if (journal.Entries.Count < 2)
            {
                throw new InvalidOperationException(
                    "A ledger journal must have at least two entries.");
            }

            if (journal.Entries.Any(e =>
                e.Amount <= 0 ||
                decimal.Round(e.Amount, 2) != e.Amount))
            {
                throw new InvalidOperationException(
                    "Journal entries must have positive amounts with no more than two decimal places.");
            }

            if (journal.Entries.Any(e =>
                e.EntryType != "Debit" &&
                e.EntryType != "Credit"))
            {
                throw new InvalidOperationException(
                    "Invalid journal entry type.");
            }

            var debits = journal.Entries
                .Where(e => e.EntryType == "Debit")
                .Sum(e => e.Amount);

            var credits = journal.Entries
                .Where(e => e.EntryType == "Credit")
                .Sum(e => e.Amount);

            if (debits != credits)
            {
                throw new InvalidOperationException(
                    "Ledger journal is not balanced.");
            }
        }

        // ================================================
        // SYSTEM ACCOUNTS INSIDE TRANSACTION
        // ================================================

        private async Task EnsureSystemAccountsInsideTransactionAsync()
        {
            await GetOrCreateSystemAccountAsync(
                "TEST_FUNDING_ZAR",
                "Development Test Funding",
                "Asset",
                "ZAR");

            await GetOrCreateSystemAccountAsync(
                "DEPOSIT_CLEARING_ZAR",
                "Deposit Clearing",
                "Asset",
                "ZAR");

            await GetOrCreateSystemAccountAsync(
                "MERCHANT_CLEARING_ZAR",
                "Merchant Payment Clearing",
                "Liability",
                "ZAR");

            await GetOrCreateSystemAccountAsync(
                "WITHDRAWAL_CLEARING_ZAR",
                "Withdrawal Clearing",
                "Liability",
                "ZAR");

            await _context.SaveChangesAsync();
        }

        // ================================================
        // CREATE WALLET ACCOUNT INSIDE TRANSACTION
        // ================================================

        private async Task<LedgerAccount>
            GetOrCreateWalletAccountInsideTransactionAsync(Wallet wallet)
        {
            var account = await _context.LedgerAccounts
                .FirstOrDefaultAsync(a => a.WalletId == wallet.Id);

            if (account != null)
            {
                ValidateWalletAccount(account, wallet);
                return account;
            }

            account = CreateWalletAccount(wallet);

            await _context.LedgerAccounts.AddAsync(account);
            await _context.SaveChangesAsync();

            return account;
        }

        // ================================================
        // DETERMINE DEBIT ACCOUNT
        // ================================================

        private async Task<LedgerAccount> GetDebitAccountAsync(
            WalletTransaction walletTransaction)
        {
            var type = walletTransaction.Type.Trim().ToLowerInvariant();

            switch (type)
            {
                case "deposit":
                    if (walletTransaction.Description.Contains(
                        "Simulated development test deposit",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return await GetSystemAccountByCodeAsync(
                            "TEST_FUNDING_ZAR");
                    }

                    return await GetSystemAccountByCodeAsync(
                        "DEPOSIT_CLEARING_ZAR");

                case "payment":
                case "withdrawal":
                    return await GetWalletAccountByIdAsync(
                        walletTransaction.WalletId);

                case "refund":
                    return await GetSystemAccountByCodeAsync(
                        "MERCHANT_CLEARING_ZAR");

                default:
                    throw new InvalidOperationException(
                        $"Unsupported transaction type: {type}");
            }
        }

        // ================================================
        // DETERMINE CREDIT ACCOUNT
        // ================================================

        private async Task<LedgerAccount> GetCreditAccountAsync(
            WalletTransaction walletTransaction)
        {
            var type = walletTransaction.Type.Trim().ToLowerInvariant();

            switch (type)
            {
                case "deposit":
                case "refund":
                    return await GetWalletAccountByIdAsync(
                        walletTransaction.WalletId);

                case "payment":
                    return await GetSystemAccountByCodeAsync(
                        "MERCHANT_CLEARING_ZAR");

                case "withdrawal":
                    return await GetSystemAccountByCodeAsync(
                        "WITHDRAWAL_CLEARING_ZAR");

                default:
                    throw new InvalidOperationException(
                        $"Unsupported transaction type: {type}");
            }
        }

        // ================================================
        // GET WALLET ACCOUNT
        // ================================================

        private async Task<LedgerAccount> GetWalletAccountByIdAsync(
            int walletId)
        {
            var account = await _context.LedgerAccounts
                .FirstOrDefaultAsync(a => a.WalletId == walletId);

            if (account == null)
            {
                throw new InvalidOperationException(
                    $"Ledger account for wallet {walletId} was not found.");
            }

            return account;
        }

        // ================================================
        // GET SYSTEM ACCOUNT
        // ================================================

        private async Task<LedgerAccount> GetSystemAccountByCodeAsync(
            string code)
        {
            var account = await _context.LedgerAccounts
                .FirstOrDefaultAsync(a => a.Code == code);

            if (account == null)
            {
                throw new InvalidOperationException(
                    $"System account {code} was not found.");
            }

            return account;
        }
    }
}
