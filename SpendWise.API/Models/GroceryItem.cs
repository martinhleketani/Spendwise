using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWise.API.Models
{
    public class GroceryItem
    {
        [Key]
        public int Id { get; set; }

        public int GroceryListId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal TotalPrice => Quantity * UnitPrice;

        public bool IsPurchased { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public GroceryList GroceryList { get; set; } = null!;
    }
}