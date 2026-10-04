using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWise.API.Models
{
    public class PasswordResetCode
    {
        // =====================================================
        // PRIMARY KEY
        // =====================================================

        [Key]
        public int Id { get; set; }


        // =====================================================
        // USER
        // =====================================================

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;


        // =====================================================
        // OTP CODE HASH
        // =====================================================

        [Required]
        public string CodeHash { get; set; } = string.Empty;


        // =====================================================
        // DELIVERY METHOD
        // =====================================================

        [Required]
        [MaxLength(20)]
        public string DeliveryMethod { get; set; } = "Email";


        // =====================================================
        // OTP EXPIRY
        // =====================================================

        public DateTime ExpiresAt { get; set; }


        // =====================================================
        // OTP USED STATUS
        // =====================================================

        public bool IsUsed { get; set; } = false;


        // =====================================================
        // DATE OTP WAS USED / INVALIDATED
        // =====================================================

        public DateTime? UsedAt { get; set; }


        // =====================================================
        // VERIFICATION ATTEMPTS
        // =====================================================

        public int AttemptCount { get; set; } = 0;


        // =====================================================
        // MAXIMUM VERIFICATION ATTEMPTS
        // =====================================================

        public int MaxAttempts { get; set; } = 5;


        // =====================================================
        // RESET TOKEN HASH
        // =====================================================

        public string? ResetTokenHash { get; set; }


        // =====================================================
        // RESET TOKEN EXPIRY
        // =====================================================

        public DateTime? ResetTokenExpiresAt { get; set; }


        // =====================================================
        // RESET TOKEN USED STATUS
        // =====================================================

        public bool ResetTokenUsed { get; set; } = false;


        // =====================================================
        // CREATED DATE
        // =====================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}