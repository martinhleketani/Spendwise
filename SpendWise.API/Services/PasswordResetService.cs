using Microsoft.AspNetCore.Identity;
using SpendWise.API.Models;
using System.Security.Cryptography;

namespace SpendWise.API.Services
{
    public class PasswordResetService
    {
        private readonly PasswordHasher<PasswordResetCode> _hasher;

        public PasswordResetService()
        {
            _hasher = new PasswordHasher<PasswordResetCode>();
        }


        // =====================================================
        // GENERATE 6-DIGIT OTP
        // =====================================================

        public string GenerateCode()
        {
            int number = RandomNumberGenerator.GetInt32(
                100000,
                1000000);

            return number.ToString();
        }


        // =====================================================
        // HASH OTP
        // =====================================================

        public string HashCode(
            PasswordResetCode resetCode,
            string code)
        {
            return _hasher.HashPassword(
                resetCode,
                code);
        }


        // =====================================================
        // VERIFY OTP
        // =====================================================

        public bool VerifyCode(
            PasswordResetCode resetCode,
            string enteredCode)
        {
            var result =
                _hasher.VerifyHashedPassword(
                    resetCode,
                    resetCode.CodeHash,
                    enteredCode);

            return result !=
                PasswordVerificationResult.Failed;
        }


        // =====================================================
        // CHECK OTP EXPIRY
        // =====================================================

        public bool IsExpired(
            PasswordResetCode resetCode)
        {
            return DateTime.UtcNow >=
                resetCode.ExpiresAt;
        }


        // =====================================================
        // GENERATE SECURE RESET TOKEN
        // =====================================================

        public string GenerateResetToken()
        {
            byte[] tokenBytes =
                RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(
                tokenBytes);
        }


        // =====================================================
        // HASH RESET TOKEN
        // =====================================================

        public string HashResetToken(
            PasswordResetCode resetCode,
            string resetToken)
        {
            return _hasher.HashPassword(
                resetCode,
                resetToken);
        }


        // =====================================================
        // VERIFY RESET TOKEN
        // =====================================================

        public bool VerifyResetToken(
            PasswordResetCode resetCode,
            string resetToken)
        {
            if (string.IsNullOrWhiteSpace(
                resetCode.ResetTokenHash))
            {
                return false;
            }

            var result =
                _hasher.VerifyHashedPassword(
                    resetCode,
                    resetCode.ResetTokenHash,
                    resetToken);

            return result !=
                PasswordVerificationResult.Failed;
        }
    }
}