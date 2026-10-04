using System.ComponentModel.DataAnnotations;

namespace SpendWise.API.DTOs
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Reset token is required.")]
        [MaxLength(
            500,
            ErrorMessage = "Reset token is invalid.")]
        public string ResetToken { get; set; } = string.Empty;


        [Required(ErrorMessage = "New password is required.")]
        [MinLength(
            8,
            ErrorMessage = "Password must contain at least 8 characters.")]
        [MaxLength(
            100,
            ErrorMessage = "Password cannot exceed 100 characters.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
            ErrorMessage =
                "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string NewPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please confirm your new password.")]
        [Compare(
            nameof(NewPassword),
            ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}