
using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.Models
{
    public class LedgerAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Asset, Liability, Equity, Income or Expense.
        [Required]
        [MaxLength(30)]
        public string AccountType { get; set; } = string.Empty;

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "ZAR";

        // Customer ledger accounts are linked to wallets.
        // System ledger accounts have a null WalletId.
        public int? WalletId { get; set; }

        public bool IsSystemAccount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Wallet? Wallet { get; set; }

        public ICollection<LedgerEntry> Entries { get; set; }
            = new List<LedgerEntry>();
    }
}
