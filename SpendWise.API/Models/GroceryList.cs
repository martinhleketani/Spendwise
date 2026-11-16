
using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.Models
{
    public class GroceryList
    {
        [Key]
        public int Id { get; set; }

        // Customer who owns the grocery list.
        [Required]
        public int UserId { get; set; }

        // Retailer where groceries will be purchased.
        [Required]
        [MaxLength(50)]
        public string Retailer { get; set; } = string.Empty;

        // Name given to the grocery list.
        [Required]
        [MaxLength(100)]
        public string ListName { get; set; } = string.Empty;

        // List status: Active, Completed or Archived.
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        // Date the grocery list was created.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Last time the grocery list was updated.
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Customer relationship.
        public User User { get; set; } = null!;

        // Items belonging to this grocery list.
        public ICollection<GroceryItem> Items { get; set; }
            = new List<GroceryItem>();
    }
}
