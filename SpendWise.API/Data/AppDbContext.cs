
using Microsoft.EntityFrameworkCore;
using SpendWise.API.Models;

namespace SpendWise.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ==========================================
        // DATABASE TABLES
        // ==========================================

        public DbSet<User> Users { get; set; }

        public DbSet<AdminUser> AdminUsers { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<MonthlyBudget> MonthlyBudgets { get; set; }

        public DbSet<CategoryBudget> CategoryBudgets { get; set; }

        public DbSet<GroceryList> GroceryLists { get; set; }

        public DbSet<GroceryItem> GroceryItems { get; set; }

        public DbSet<PasswordResetCode> PasswordResetCodes { get; set; }

        public DbSet<BudgetNotification> BudgetNotifications { get; set; }

        // ==========================================
        // WALLET TABLES
        // ==========================================

        public DbSet<Wallet> Wallets { get; set; }

        public DbSet<WalletTransaction> WalletTransactions { get; set; }

        // ==========================================
        // MERCHANT TABLES
        // ==========================================

        public DbSet<Merchant> Merchants { get; set; }

        // ==========================================
        // DOUBLE-ENTRY LEDGER TABLES
        // ==========================================

        public DbSet<LedgerAccount> LedgerAccounts { get; set; }

        public DbSet<LedgerJournal> LedgerJournals { get; set; }

        public DbSet<LedgerEntry> LedgerEntries { get; set; }

        // ==========================================
        // AUDIT LOG TABLES
        // ==========================================

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // ADMIN USERS
            // ==========================================

            modelBuilder.Entity<AdminUser>()
                .Property(a => a.FullName)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<AdminUser>()
                .Property(a => a.Email)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<AdminUser>()
                .Property(a => a.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            modelBuilder.Entity<AdminUser>()
                .HasIndex(a => a.Email)
                .IsUnique()
                .HasDatabaseName("IX_AdminUsers_Email");

            // ==========================================
            // TRANSACTIONS
            // ==========================================

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.UserId)
                .HasDatabaseName("IX_Transactions_UserId");

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => new
                {
                    t.UserId,
                    t.RequestId
                })
                .HasDatabaseName("IX_Transactions_UserId_RequestId")
                .IsUnique();

            // ==========================================
            // MONTHLY BUDGET
            // ==========================================

            modelBuilder.Entity<MonthlyBudget>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MonthlyBudget>()
                .HasIndex(b => new
                {
                    b.UserId,
                    b.Month,
                    b.Year
                })
                .IsUnique();

            // ==========================================
            // CATEGORY BUDGET
            // ==========================================

            modelBuilder.Entity<CategoryBudget>()
                .HasOne(c => c.MonthlyBudget)
                .WithMany(b => b.CategoryBudgets)
                .HasForeignKey(c => c.MonthlyBudgetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CategoryBudget>()
                .HasIndex(c => new
                {
                    c.MonthlyBudgetId,
                    c.Category
                })
                .IsUnique();

            // ==========================================
            // GROCERY LIST
            // ==========================================

            modelBuilder.Entity<GroceryList>()
                .HasOne(g => g.User)
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // GROCERY ITEM
            // ==========================================

            modelBuilder.Entity<GroceryItem>()
                .HasOne(i => i.GroceryList)
                .WithMany(g => g.Items)
                .HasForeignKey(i => i.GroceryListId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // PASSWORD RESET CODE
            // ==========================================

            modelBuilder.Entity<PasswordResetCode>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetCode>()
                .HasIndex(r => r.UserId)
                .HasDatabaseName("IX_PasswordResetCodes_UserId");

            modelBuilder.Entity<PasswordResetCode>()
                .HasIndex(r => new
                {
                    r.UserId,
                    r.IsUsed,
                    r.ExpiresAt
                })
                .HasDatabaseName(
                    "IX_PasswordResetCodes_UserId_IsUsed_ExpiresAt");

            // ==========================================
            // BUDGET NOTIFICATIONS
            // ==========================================

            modelBuilder.Entity<BudgetNotification>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BudgetNotification>()
                .Property(n => n.Category)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<BudgetNotification>()
                .Property(n => n.Message)
                .HasMaxLength(500)
                .IsRequired();

            modelBuilder.Entity<BudgetNotification>()
                .HasIndex(n => new
                {
                    n.UserId,
                    n.Category,
                    n.Month,
                    n.Year,
                    n.Threshold
                })
                .HasDatabaseName("IX_BudgetNotifications_UniqueAlert")
                .IsUnique();

            modelBuilder.Entity<BudgetNotification>()
                .HasIndex(n => new
                {
                    n.UserId,
                    n.CreatedAt
                })
                .HasDatabaseName("IX_BudgetNotifications_UserId_CreatedAt");

            // ==========================================
            // WALLETS
            // ==========================================

            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.UserId)
                .IsUnique()
                .HasDatabaseName("IX_Wallets_UserId");

            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.WalletNumber)
                .IsUnique()
                .HasDatabaseName("IX_Wallets_WalletNumber");

            modelBuilder.Entity<Wallet>()
                .Property(w => w.Currency)
                .HasMaxLength(3)
                .IsRequired();

            modelBuilder.Entity<Wallet>()
                .Property(w => w.Status)
                .HasMaxLength(20)
                .IsRequired();

            // ==========================================
            // WALLET TRANSACTIONS
            // ==========================================

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(t => t.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.WalletId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WalletTransaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<WalletTransaction>()
                .Property(t => t.Type)
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<WalletTransaction>()
                .Property(t => t.Status)
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<WalletTransaction>()
                .Property(t => t.Currency)
                .HasMaxLength(3)
                .IsRequired();

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(t => t.Reference)
                .IsUnique()
                .HasDatabaseName("IX_WalletTransactions_Reference");

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(t => t.ExternalReference)
                .IsUnique()
                .HasDatabaseName("IX_WalletTransactions_ExternalReference");

            modelBuilder.Entity<WalletTransaction>()
                .Property(t => t.IdempotencyKey)
                .HasMaxLength(100)
                .IsRequired(false);

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(t => new
                {
                    t.WalletId,
                    t.IdempotencyKey
                })
                .IsUnique()
                .HasDatabaseName(
                    "IX_WalletTransactions_WalletId_IdempotencyKey");

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(t => new
                {
                    t.WalletId,
                    t.CreatedAt
                })
                .HasDatabaseName(
                    "IX_WalletTransactions_WalletId_CreatedAt");

            // ==========================================
            // MERCHANTS
            // ==========================================

            modelBuilder.Entity<Merchant>()
                .Property(m => m.BusinessName)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<Merchant>()
                .Property(m => m.ContactPerson)
                .HasMaxLength(100);

            modelBuilder.Entity<Merchant>()
                .Property(m => m.Email)
                .HasMaxLength(150);

            modelBuilder.Entity<Merchant>()
                .Property(m => m.PhoneNumber)
                .HasMaxLength(20);

            modelBuilder.Entity<Merchant>()
                .Property(m => m.BusinessAddress)
                .HasMaxLength(300);

            modelBuilder.Entity<Merchant>()
                .Property(m => m.Status)
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<Merchant>()
                .HasIndex(m => m.Status)
                .HasDatabaseName("IX_Merchants_Status");

            // ==========================================
            // LEDGER ACCOUNTS
            // ==========================================

            modelBuilder.Entity<LedgerAccount>()
                .HasOne(a => a.Wallet)
                .WithMany()
                .HasForeignKey(a => a.WalletId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LedgerAccount>()
                .Property(a => a.Code)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<LedgerAccount>()
                .Property(a => a.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<LedgerAccount>()
                .Property(a => a.AccountType)
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<LedgerAccount>()
                .Property(a => a.Currency)
                .HasMaxLength(3)
                .IsRequired();

            modelBuilder.Entity<LedgerAccount>()
                .HasIndex(a => a.Code)
                .IsUnique()
                .HasDatabaseName("IX_LedgerAccounts_Code");

            modelBuilder.Entity<LedgerAccount>()
                .HasIndex(a => a.WalletId)
                .IsUnique()
                .HasDatabaseName("IX_LedgerAccounts_WalletId");

            // ==========================================
            // LEDGER JOURNALS
            // ==========================================

            modelBuilder.Entity<LedgerJournal>()
                .HasOne(j => j.WalletTransaction)
                .WithOne()
                .HasForeignKey<LedgerJournal>(
                    j => j.WalletTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LedgerJournal>()
                .Property(j => j.Reference)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<LedgerJournal>()
                .Property(j => j.Description)
                .HasMaxLength(250)
                .IsRequired();

            modelBuilder.Entity<LedgerJournal>()
                .Property(j => j.Currency)
                .HasMaxLength(3)
                .IsRequired();

            modelBuilder.Entity<LedgerJournal>()
                .Property(j => j.Status)
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<LedgerJournal>()
                .HasIndex(j => j.Reference)
                .IsUnique()
                .HasDatabaseName("IX_LedgerJournals_Reference");

            modelBuilder.Entity<LedgerJournal>()
                .HasIndex(j => j.WalletTransactionId)
                .IsUnique()
                .HasDatabaseName(
                    "IX_LedgerJournals_WalletTransactionId");

            // ==========================================
            // LEDGER ENTRIES
            // ==========================================

            modelBuilder.Entity<LedgerEntry>()
                .HasOne(e => e.LedgerJournal)
                .WithMany(j => j.Entries)
                .HasForeignKey(e => e.LedgerJournalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LedgerEntry>()
                .HasOne(e => e.LedgerAccount)
                .WithMany(a => a.Entries)
                .HasForeignKey(e => e.LedgerAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LedgerEntry>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LedgerEntry>()
                .Property(e => e.EntryType)
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<LedgerEntry>()
                .HasIndex(e => e.LedgerJournalId)
                .HasDatabaseName(
                    "IX_LedgerEntries_LedgerJournalId");

            modelBuilder.Entity<LedgerEntry>()
                .HasIndex(e => e.LedgerAccountId)
                .HasDatabaseName(
                    "IX_LedgerEntries_LedgerAccountId");

            // ==========================================
            // AUDIT LOGS
            // ==========================================

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.AdminName)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.AdminEmail)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Action)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Module)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Description)
                .HasMaxLength(1000)
                .IsRequired();

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.EntityType)
                .HasMaxLength(100);

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.EntityId)
                .HasMaxLength(100);

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.IpAddress)
                .HasMaxLength(50);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedAt)
                .HasDatabaseName("IX_AuditLogs_CreatedAt");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.AdminUserId)
                .HasDatabaseName("IX_AuditLogs_AdminUserId");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Module)
                .HasDatabaseName("IX_AuditLogs_Module");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Action)
                .HasDatabaseName("IX_AuditLogs_Action");
        }
    }
}
