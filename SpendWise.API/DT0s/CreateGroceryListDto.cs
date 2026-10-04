using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.DTOs
{
    public class CreateGroceryListDto
    {
        [Required(ErrorMessage = "List name is required.")]
        [MaxLength(
            100,
            ErrorMessage = "List name cannot exceed 100 characters.")]
        public string ListName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Retailer is required.")]
        [MaxLength(
            50,
            ErrorMessage = "Retailer cannot exceed 50 characters.")]
        public string Retailer { get; set; } = string.Empty;
    }
}