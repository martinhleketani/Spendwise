
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWise.API.Models
{
    public class Wallet
    {
        [Key]
        public int Id { get; set; }

        // Customer who owns this wallet.
        [Required]
        public int UserId { get; set; }

        // Unique wallet account number.
        [Required]
        [MaxLength(30)]
        public string WalletNumber { get; set; } = string.Empty;

        // ISO currency code.
        // Examples: ZAR, USD, GBP, EUR.
        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "ZAR";

        // Active, Suspended or Closed.
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        // Wallet creation date.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Last wallet update.
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Customer relationship.
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        // Wallet financial transaction history.
        public ICollection<WalletTransaction> Transactions { get; set; }
            = new List<WalletTransaction>();
    }
}
