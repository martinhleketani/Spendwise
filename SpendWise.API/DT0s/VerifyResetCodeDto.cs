using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.DTOs
{
    public class VerifyResetCodeDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(
            ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(
            150,
            ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reset code is required.")]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "Reset code must contain exactly 6 digits.")]
        public string Code { get; set; } = string.Empty;
    }
}