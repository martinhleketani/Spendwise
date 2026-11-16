
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWise.API.Models
{
    public class LedgerEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LedgerJournalId { get; set; }

        [Required]
        public int LedgerAccountId { get; set; }

        // Debit or Credit.
        [Required]
        [MaxLength(10)]
        public string EntryType { get; set; } = string.Empty;

        // Always a positive amount.
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public LedgerJournal LedgerJournal { get; set; } = null!;

        public LedgerAccount LedgerAccount { get; set; } = null!;
    }
}

