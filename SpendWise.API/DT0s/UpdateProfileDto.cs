using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.DTOs
{
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(
            100,
            ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(
            ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(
            150,
            ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(
            @"^0\d{9}$",
            ErrorMessage =
                "Phone number must contain exactly 10 digits and start with 0.")]
        public string? PhoneNumber { get; set; }
    }
}