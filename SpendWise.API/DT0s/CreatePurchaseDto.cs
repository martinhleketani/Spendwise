using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.DTOs
{
    public class CreatePurchaseDto
    {
        [Required(ErrorMessage = "Category is required.")]
        [MaxLength(
            50,
            ErrorMessage = "Category cannot exceed 50 characters.")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(
            150,
            ErrorMessage = "Description cannot exceed 150 characters.")]
        public string Description { get; set; } = string.Empty;

        [Range(
            0.01,
            1000000,
            ErrorMessage = "Amount must be between R0.01 and R1,000,000.")]
        public decimal Amount { get; set; }

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

        [Required(ErrorMessage = "RequestId is required.")]
        [MaxLength(
            100,
            ErrorMessage = "RequestId cannot exceed 100 characters.")]
        public string RequestId { get; set; } = string.Empty;
    }
}