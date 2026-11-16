
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWise.API.Models
{
    public class WalletTransaction
    {
        [Key]
        public int Id { get; set; }

        // Wallet associated with this transaction.
        [Required]
        public int WalletId { get; set; }

        // Financial transaction type:
        // Deposit, Payment, Refund, Adjustment.
        [Required]
        [MaxLength(30)]
        public string Type { get; set; } = string.Empty;

        // Transaction amount.
        // Always store a positive amount.
        // Direction is determined by Type.
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Transaction category.
        [MaxLength(50)]
        public string? Category { get; set; }

        // Payment or deposit description.
        [Required]
        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        // Unique financial transaction reference.
        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        // External payment provider reference.
        [MaxLength(150)]
        public string? ExternalReference { get; set; }

        // Customer request key used to prevent
        // duplicate payment requests.
        [MaxLength(100)]
        public string? IdempotencyKey { get; set; }

        // Payment status:
        // Pending, Completed, Failed, Reversed.
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        // Currency used for this transaction.
        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "ZAR";

        // Date the transaction was created.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Date the transaction was confirmed.
        public DateTime? CompletedAt { get; set; }

        // Wallet relationship.
        [ForeignKey(nameof(WalletId))]
        public Wallet Wallet { get; set; } = null!;
    }
}
