using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWise.API.Models
{
    public class MonthlyBudget
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalLimit { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;

        public ICollection<CategoryBudget> CategoryBudgets { get; set; }
            = new List<CategoryBudget>();
    }
}