namespace SpendWise.App.Models;


// =========================================================
// FORGOT PASSWORD REQUEST
// =========================================================

public class ForgotPasswordRequest
{
    public string Email { get; set; } =
        string.Empty;
}


// =========================================================
// VERIFY RESET CODE REQUEST
// =========================================================

public class VerifyResetCodeRequest
{
    public string Email { get; set; } =
        string.Empty;

    public string Code { get; set; } =
        string.Empty;
}


// =========================================================
// VERIFY RESET CODE RESPONSE
// =========================================================

public class VerifyResetCodeResponse
{
    public string Message { get; set; } =
        string.Empty;

    public string ResetToken { get; set; } =
        string.Empty;

    public int ResetTokenExpiresInMinutes { get; set; }
}


// =========================================================
// RESET PASSWORD REQUEST
// =========================================================

public class ResetPasswordRequest
{
    public string ResetToken { get; set; } =
        string.Empty;

    public string NewPassword { get; set; } =
        string.Empty;

    public string ConfirmPassword { get; set; } =
        string.Empty;
}