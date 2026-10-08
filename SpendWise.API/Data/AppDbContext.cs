
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

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<MonthlyBudget> MonthlyBudgets { get; set; }

        public DbSet<CategoryBudget> CategoryBudgets { get; set; }

        public DbSet<GroceryList> GroceryLists { get; set; }

        public DbSet<GroceryItem> GroceryItems { get; set; }

        public DbSet<PasswordResetCode> PasswordResetCodes { get; set; }

        public DbSet<Retailer> Retailers { get; set; }

        public DbSet<RetailerProductPrice> RetailerProductPrices { get; set; }

        // NEW: BUDGET NOTIFICATIONS

        public DbSet<BudgetNotification> BudgetNotifications { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                .HasDatabaseName(
                    "IX_Transactions_UserId");

            // PURCHASE REQUEST IDEMPOTENCY

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => new
                {
                    t.UserId,
                    t.RequestId
                })
                .HasDatabaseName(
                    "IX_Transactions_UserId_RequestId")
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
                .HasDatabaseName(
                    "IX_PasswordResetCodes_UserId");

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
            // RETAILER
            // ==========================================

            modelBuilder.Entity<Retailer>()
                .Property(r => r.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Retailer>()
                .Property(r => r.WebsiteUrl)
                .HasMaxLength(500);

            modelBuilder.Entity<Retailer>()
                .HasIndex(r => r.Name)
                .HasDatabaseName(
                    "IX_Retailers_Name")
                .IsUnique();

            // ==========================================
            // RETAILER PRODUCT PRICE
            // ==========================================

            modelBuilder.Entity<RetailerProductPrice>()
                .HasOne(p => p.Retailer)
                .WithMany(r => r.ProductPrices)
                .HasForeignKey(p => p.RetailerId)
                .OnDelete(DeleteBehavior.Cascade);

            // PRODUCT NAME

            modelBuilder.Entity<RetailerProductPrice>()
                .Property(p => p.ProductName)
                .HasMaxLength(200)
                .IsRequired();

            // BRAND

            modelBuilder.Entity<RetailerProductPrice>()
                .Property(p => p.Brand)
                .HasMaxLength(100);

            // CATEGORY

            modelBuilder.Entity<RetailerProductPrice>()
                .Property(p => p.Category)
                .HasMaxLength(100);

            // SIZE

            modelBuilder.Entity<RetailerProductPrice>()
                .Property(p => p.Size)
                .HasMaxLength(100);

            // PRODUCT URL

            modelBuilder.Entity<RetailerProductPrice>()
                .Property(p => p.ProductUrl)
                .HasMaxLength(1000);

            // PRICE SOURCE

            modelBuilder.Entity<RetailerProductPrice>()
                .Property(p => p.PriceSource)
                .HasMaxLength(200);

            // CURRENT PRICE

            modelBuilder.Entity<RetailerProductPrice>()
                .Property(p => p.Price)
                .HasPrecision(10, 2);

            // REGULAR PRICE

            modelBuilder.Entity<RetailerProductPrice>()
                .Property(p => p.RegularPrice)
                .HasPrecision(10, 2);

            // RETAILER INDEX

            modelBuilder.Entity<RetailerProductPrice>()
                .HasIndex(p => p.RetailerId)
                .HasDatabaseName(
                    "IX_RetailerProductPrices_RetailerId");

            // PRODUCT NAME INDEX

            modelBuilder.Entity<RetailerProductPrice>()
                .HasIndex(p => p.ProductName)
                .HasDatabaseName(
                    "IX_RetailerProductPrices_ProductName");

            // RETAILER + PRODUCT INDEX

            modelBuilder.Entity<RetailerProductPrice>()
                .HasIndex(p => new
                {
                    p.RetailerId,
                    p.ProductName
                })
                .HasDatabaseName(
                    "IX_RetailerProductPrices_RetailerId_ProductName");

            // PRICE CHECK DATE INDEX

            modelBuilder.Entity<RetailerProductPrice>()
                .HasIndex(p => p.PriceCheckedAt)
                .HasDatabaseName(
                    "IX_RetailerProductPrices_PriceCheckedAt");

            // ==========================================
            // NEW: BUDGET NOTIFICATIONS
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

            // Prevent duplicate alerts for the same
            // user, category, month, year and threshold.

            modelBuilder.Entity<BudgetNotification>()
                .HasIndex(n => new
                {
                    n.UserId,
                    n.Category,
                    n.Month,
                    n.Year,
                    n.Threshold
                })
                .HasDatabaseName(
                    "IX_BudgetNotifications_UniqueAlert")
                .IsUnique();

            // Speed up notification history queries.

            modelBuilder.Entity<BudgetNotification>()
                .HasIndex(n => new
                {
                    n.UserId,
                    n.CreatedAt
                })
                .HasDatabaseName(
                    "IX_BudgetNotifications_UserId_CreatedAt");
        }
    }
}
