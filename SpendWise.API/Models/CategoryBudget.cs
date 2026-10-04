using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWise.API.Models
{
    public class CategoryBudget
    {
        [Key]
        public int Id { get; set; }

        public int MonthlyBudgetId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LimitAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SpentAmount { get; set; } = 0.00m;

        public MonthlyBudget MonthlyBudget { get; set; } = null!;
    }
}