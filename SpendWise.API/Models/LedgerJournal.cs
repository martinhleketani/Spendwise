
using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.Models
{
    public class LedgerJournal
    {
        [Key]
        public int Id { get; set; }

        // Links the journal to an existing wallet transaction.
        public int? WalletTransactionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "ZAR";

        // Draft, Posted or Reversed.
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PostedAt { get; set; }

        public WalletTransaction? WalletTransaction { get; set; }

        public ICollection<LedgerEntry> Entries { get; set; }
            = new List<LedgerEntry>();
    }
}
