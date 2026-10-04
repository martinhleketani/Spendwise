using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.Models
{
    public class GroceryList
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Retailer { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ListName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;

        public ICollection<GroceryItem> Items { get; set; }
            = new List<GroceryItem>();
    }
}