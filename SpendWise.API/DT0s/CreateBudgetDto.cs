
using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.DTOs
{
    public class CreateBudgetDto
    {
        [Range(
            1,
            12,
            ErrorMessage = "Month must be between 1 and 12.")]
        public int Month { get; set; }

        [Range(
            2020,
            2100,
            ErrorMessage = "Year must be between 2020 and 2100.")]
        public int Year { get; set; }

        [Range(
            0.01,
            100000000,
            ErrorMessage = "Total budget limit must be greater than R0.")]
        public decimal TotalLimit { get; set; }

        [Required(
            ErrorMessage = "At least one budget category is required.")]
        [MinLength(
            1,
            ErrorMessage = "At least one budget category is required.")]
        public List<CategoryBudgetDto> Categories { get; set; }
            = new List<CategoryBudgetDto>();
    }

    public class CategoryBudgetDto
    {
        [Required(
            ErrorMessage = "Category name is required.")]
        [MaxLength(
            50,
            ErrorMessage = "Category name cannot exceed 50 characters.")]
        public string Category { get; set; } = string.Empty;

        // FIX: Allow R0 for categories without an allocation.
        [Range(
            0,
            100000000,
            ErrorMessage = "Category limit cannot be negative.")]
        public decimal LimitAmount { get; set; }
    }
}
