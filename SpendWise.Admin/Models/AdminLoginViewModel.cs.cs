
using System.ComponentModel.DataAnnotations;

namespace SpendWise.Admin.Models
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Enter your administrator email.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
