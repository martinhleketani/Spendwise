using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.DTOs
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(
            ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(
            150,
            ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;
    }
}