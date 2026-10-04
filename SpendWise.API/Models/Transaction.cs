using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWise.API.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        // Transaction belongs directly to the logged-in user.
        public int UserId { get; set; }

        // Purchase/spending amount.
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Example: Purchase
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        // Examples:
        // Groceries, Transport, Entertainment,
        // Utilities, Other
        [MaxLength(50)]
        public string? Category { get; set; }

        // Examples:
        // Checkers, Pick n Pay, Shoprite,
        // SPAR, Woolworths Food
        [MaxLength(100)]
        public string? Retailer { get; set; }

        // Prevents the same purchase request
        // from being recorded more than once.
        [MaxLength(100)]
        public string? RequestId { get; set; }

        // Human-readable transaction description.
        [Required]
        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        // Transaction reference.
        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        // Date and time the transaction was recorded.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // User who owns this transaction.
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
