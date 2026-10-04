using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.DTOs
{
    public class AddGroceryItemDto
    {
        [Required(ErrorMessage = "Product name is required.")]
        [MaxLength(
            100,
            ErrorMessage = "Product name cannot exceed 100 characters.")]
        public string ProductName { get; set; } = string.Empty;

        [Range(
            1,
            1000,
            ErrorMessage = "Quantity must be between 1 and 1,000.")]
        public int Quantity { get; set; }

        [Range(
            0.01,
            1000000,
            ErrorMessage = "Unit price must be between R0.01 and R1,000,000.")]
        public decimal UnitPrice { get; set; }
    }
}